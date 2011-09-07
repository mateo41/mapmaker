<%@ Application Language="C#" %>

<script runat="server">

    void Application_Start(object sender, EventArgs e) 
    {
        // Code that runs on application startup

    }
    
    void Application_End(object sender, EventArgs e) 
    {
        //  Code that runs on application shutdown

    }
        
    void Application_Error(object sender, EventArgs e) 
    { 
        // Code that runs when an unhandled error occurs

    }

    void Session_Start(object sender, EventArgs e) 
    {
        // Code that runs when a new session is started

    }

    void Session_End(object sender, EventArgs e) 
    {
        // Code that runs when a session ends. 
        // Note: The Session_End event is raised only when the sessionstate mode
        // is set to InProc in the Web.config file. If session mode is set to StateServer 
        // or SQLServer, the event is not raised.
        // Close out session and quit application
        System.Collections.Generic.List<ESRI.ArcGIS.Server.IServerContext> contexts = new System.Collections.Generic.List<ESRI.ArcGIS.Server.IServerContext>();
        for (int i = 0; i < Session.Count; i++)
        {
            if (Session[i] is ESRI.ArcGIS.Server.IServerContext)
                contexts.Add((ESRI.ArcGIS.Server.IServerContext)Session[i]);
            else if (Session[i] is IDisposable)
                ((IDisposable)Session[i]).Dispose();
        }

        foreach (ESRI.ArcGIS.Server.IServerContext context in contexts)
        {
            context.RemoveAll();
            context.ReleaseContext();
        }
    }
    
    void Session_Abandon(object sender, EventArgs e)
   {
       // Code that runs when a session is abandoned. 
       System.Collections.Generic.List<ESRI.ArcGIS.Server.IServerContext> contexts = new System.Collections.Generic.List<ESRI.ArcGIS.Server.IServerContext>();
       for (int i = 0; i < Session.Count; i++)
       {
           if (Session[i] is ESRI.ArcGIS.Server.IServerContext)
               contexts.Add((ESRI.ArcGIS.Server.IServerContext)Session[i]);
           else if (Session[i] is IDisposable)
               ((IDisposable)Session[i]).Dispose();
       }

       foreach (ESRI.ArcGIS.Server.IServerContext context in contexts)
       {
           context.RemoveAll();
           context.ReleaseContext();
       }
   }    
       
</script>
