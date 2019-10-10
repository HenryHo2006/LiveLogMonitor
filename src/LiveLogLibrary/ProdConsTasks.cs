using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace LiveLogMonitor
{
    /// <summary>
    /// 生产消费模式任务管理器
    /// </summary>
    /// <remarks>
    /// 生产消费模式任务管理器是生产/消费模型的泛型类任务：
    /// 一、一个或多个任务监视“原料”集合(TMaterial)，有则处理，无则等待
    /// 二、处理“原料”的算法由外界通过setter提供，可动态改变，降低耦合
    /// 三、收尾工作由外界通过setter提供
    /// 四、启动、中止、停止由外界决定时机(注意中止和停止的区别)
    /// 五、处理“原料”时不允许抛异常
    /// 备注：
    /// (1)、本类多线程操作安全
    /// (2)、多个实例化的生产消费模式任务管理器可以组成生产流水线，例如：板边识别 => 特征对位 => 轮廓提取 => SIP处理
    /// </remarks>
    public sealed class ProdConsTasks<TMaterial> : IDisposable
    {
        #region 成员变量和属性

        private BlockingCollection<TMaterial> _materials;               // 原材料池
        private Task[] _tasks;                                          // 任务s
        private ManualResetEvent[] _events;                             // 启动事件和终止事件
        private CancellationTokenSource _cancel;                        // 取消器

        /// <summary>
        /// 处理函数,必须在材料进来前提供,可动态改变
        /// (材料，任务id，取消令牌)
        /// </summary>
        public Action<TMaterial, int, CancellationToken> Process { private get; set; }

        /// <summary>
        /// 终结器，可以不提供
        /// </summary>
        public Action<bool> Terminal { private get; set; }

        /// <summary>
        /// 无材料继续模式
        /// </summary>
        /// <remarks>
        /// 这个模式也称为批量处理模式，有些场景需要把材料池取空，然后去批量处理
        /// </remarks>
        public bool NoMaterialContinue { get; set; }

        /// <summary>
        /// 任务名称
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// 内部任务数量
        /// </summary>
        public int TaskNumber => _tasks.Length;

        #endregion

        #region 构造函数

        /// <summary>
        /// 构造内核处理器
        /// </summary>
        /// <param name="task_name"></param>
        /// <param name="task_count">任务数量</param>
        public ProdConsTasks(string task_name, int task_count = 1)
        {
            Name = task_name;
            _cancel = new CancellationTokenSource();
            _materials = new BlockingCollection<TMaterial>();   // AOI适合先进先出
            _tasks = new Task[task_count];
            for (int i = 0; i < _tasks.Length; i++)
            {
                _tasks[i] = new Task(waitAndDo, i, _cancel.Token);
            }
            _events = new[] { new ManualResetEvent(false), new ManualResetEvent(false) };
        }

        #endregion

        #region 私有函数

        // 来料加工，可取消，可结束
        private void waitAndDo(object obj)
        {
            int task_id = (int)obj;

            bool abort = false;
            try
            {
                while (true)
                {
                    TMaterial material;
                    if (NoMaterialContinue)
                    {
                        _cancel.Token.ThrowIfCancellationRequested();
                        if (!_materials.TryTake(out material))
                        {
                            if (_materials.IsAddingCompleted) break;
                            material = default(TMaterial);
                        }
                    }
                    else
                    {
                        material = _materials.Take(_cancel.Token);
                    }
                    Debug.Assert(Process != null, "must provide Process function");
                    Process(material, task_id, _cancel.Token);
                }
            }
            catch (OperationCanceledException)
            {
                // OperationCanceledException意味着Take()操作时，发生了取消操作
                abort = true;
            }
            catch (InvalidOperationException)
            {
                // InvalidOperationException意味着Take()在等候时，集合被设置了完成标志
            }
            Terminal?.Invoke(abort);
        }

        #endregion

        #region 基本函数重载

        /// <summary>
        /// ToString
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return $"task:{Name}({_tasks.Length}), materials:{_materials.Count}";
        }

        #endregion

        #region 公共函数

        /// <summary>
        /// 启动任务
        /// </summary>
        public void Start()
        {
            foreach (var task in _tasks)
                task.Start();
            _events[0].Set();
        }

        /// <summary>
        /// 中止处理(并非中断线程)
        /// </summary>
        public void Abort()
        {
            _cancel.Cancel();
            _events[1].Set();
        }

        /// <summary>
        /// 添加待处理的材料
        /// </summary>
        /// <remarks>
        /// 调用过StopEnabled后，不可再添加材料
        /// </remarks>
        /// <param name="material"></param>
        public void AddMaterial(TMaterial material)
        {
            _materials.Add(material);
        }

        /// <summary>
        /// 材料已全部添加完。处理完成可以退出
        /// </summary>
        public void CompleteAddMaterial()
        {
            _materials.CompleteAdding();
        }

        /// <summary>
        /// 等待全部任务运行完成
        /// </summary>
        /// <param name="stop_enabled">不再提供新材料</param>
        /// <returns>是否有子任务被终止</returns>
        public async Task<bool> WaitAllFinishedAsync(bool stop_enabled = true)
        {
            if (stop_enabled) CompleteAddMaterial();
            // 等待任务启动信号或中止信号，有一个就继续
            // 如果任务未启动也未终止，则一直等待
            int index = WaitHandle.WaitAny(_events.Select(e => e as WaitHandle).ToArray());
            if (index == 0)  // 只要任务启动就等待全部结束
            {
                try
                {
                    await Task.WhenAll(_tasks);
                }
                catch (TaskCanceledException)
                {
                    // 任务可能被取消，这是正常的
                }
            }

            return _cancel.IsCancellationRequested;
        }

        #endregion

        #region Dispose模式

        private bool _disposed;

        /// <summary>
        /// Implement IDisposable. 
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
        }

        /// <summary>
        /// Dispose
        /// </summary>
        private void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    // Free other state (managed objects).
                    _tasks = null;
                    if (_materials != null)
                    {
                        _materials.Dispose();
                        _materials = null;
                    }
                    if (_cancel != null)
                    {
                        _cancel.Dispose();
                        _cancel = null;
                    }
                    if (_events != null)
                    {
                        _events[0].Dispose();
                        _events[1].Dispose();
                        _events = null;
                    }
                }
                // Free your own state (unmanaged objects).
                // Set large fields to null.
                _disposed = true;
            }
        }

        #endregion

    }
}
