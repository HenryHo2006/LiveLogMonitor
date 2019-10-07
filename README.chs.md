# LiveLogMonitor

动态Log监视器，基于.net standard 2.0, 跨平台    

Log最主要的两类应用场景：    

-  ### Archived Log    
     生产中，应用程序出现了异常情形崩溃了，然后客户重启应用程序继续生产，需要售服人员或研发人员排查情况    
     解决方案：软件输出日志到Windows Event Log System，默认开放级别是Error, Fault    
     如果在.Net技术栈中，用Log4Net的EventLogAppender即可，用log4net.config来配置    
     备注：这种解决方案基本上可以定位到问题发生的模块及抛出异常的代码行，但不一定能完全解决问题    

-  ### Living Log    
     研发人员在线跟进试用期软件时，对某些错误、流程、性能，需要及时监控，收集信息    
     解决方案：应用程序运行时，创建一个命名管道用于log monitoring    
     用LiveLogWatching应用程序，不带参数则枚举本地的命名管道，找到首个Log开头的连接    
     带参数则用参数来连接    

### 用法：

在程序中增加两句代码创建一个命名管道，LiveLogMonitor程序会自动来连接并显示Log
```
 var pipe = Utils.CreatePipe();
 var task = Utils.WaitConnectAndBrokenAsync(pipe, exit.Token);
```

请参考附带的两个例子
