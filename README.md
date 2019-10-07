# LiveLogMonitor
Live Log Monitor for dotnet, cross platform, console program


There are two main types of application scenarios for log:

-  ### Archived Log    
   In production, the application crashed, and then the client restarts the application to continue production, and needs to be solve by R&D person.    
     
-  ### Living Log    
   Living log are required when R&D personnel need to solve a specific problem online    
   Log4net.config is cumbersome to configure and is not conducive to solving problems quickly    

### Usage£º
   Add two lines of code to the program to create a named pipe£¬
LiveLogMonitor program will be connect and display Log    
```
 var pipe = Utils.CreatePipe();
 var task = Utils.WaitConnectAndBrokenAsync(pipe, exit.Token);
```

reference the sample projects for detail

