# LiveLogMonitor

��̬Log������������.net standard 2.0, ��ƽ̨    

Log����Ҫ������Ӧ�ó�����    

-  ### Archived Log    
     �����У�Ӧ�ó���������쳣���α����ˣ�Ȼ��ͻ�����Ӧ�ó��������������Ҫ�۷���Ա���з���Ա�Ų����    
     �����������������־��Windows Event Log System��Ĭ�Ͽ��ż�����Error, Fault    
     �����.Net����ջ�У���Log4Net��EventLogAppender���ɣ���log4net.config������    
     ��ע�����ֽ�����������Ͽ��Զ�λ�����ⷢ����ģ�鼰�׳��쳣�Ĵ����У�����һ������ȫ�������    

-  ### Living Log    
     �з���Ա���߸������������ʱ����ĳЩ�������̡����ܣ���Ҫ��ʱ��أ��ռ���Ϣ    
     ���������Ӧ�ó�������ʱ������һ�������ܵ�����log monitoring    
     ��LiveLogWatchingӦ�ó��򣬲���������ö�ٱ��ص������ܵ����ҵ��׸�Log��ͷ������    
     ���������ò���������    

### �÷���

�ڳ���������������봴��һ�������ܵ���LiveLogMonitor������Զ������Ӳ���ʾLog
```
 var pipe = Utils.CreatePipe();
 var task = Utils.WaitConnectAndBrokenAsync(pipe, exit.Token);
```

��ο���������������
