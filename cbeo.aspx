<%@ Page Language="C#" AutoEventWireup="true" Async="true" CodeFile="cbeo.aspx.cs" Inherits="map" %>



<%@ Register Assembly="ESRI.ArcGIS.ADF.Web.UI.WebControls, Version=9.3.0.1770, Culture=neutral, PublicKeyToken=8fc3cc631e44ad86"
    Namespace="ESRI.ArcGIS.ADF.Web.UI.WebControls" TagPrefix="esri" %>


<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">



<html xmlns="http://www.w3.org/1999/xhtml" >
<head runat="server">
    <title>Map Maker</title>
</head>





<body bgcolor="#999999" >
        
    
    <form id="form1" runat="server" >
        
    
  <script language="javascript" type="text/javascript">
    function postBackByObject(mEvent)
{
    var o;
    // Internet Explorer    
    if (mEvent.srcElement)
    {
        o = mEvent.srcElement;
    }
    // Netscape and Firefox
    else if (mEvent.target)
    {
        o = mEvent.target;
    }

    //var o = window.event.srcElement;
    if (o.tagName == "INPUT" && o.type == "checkbox")
    {
       __doPostBack("UpdatePanel1","");
       //__doPostBack("","");
    }
}

    </script>
      
 
      
	
    <asp:ScriptManager 
            ID="ScriptManager1" runat="server" EnablePartialRendering="true"
           AsyncPostBackTimeout="600">
        
        </asp:ScriptManager>
	<input type="hidden" id="hid_selected_lang" name="hid_selected_lang"/>
	
	  <script language="javascript" type="text/javascript">
        var xPos, yPos;
        var prm = Sys.WebForms.PageRequestManager.getInstance();
      
        function BeginRequestHandler(sender,args) {
            if ($get('<%= Panel1.ClientID %>') != null) {
                xPos = $get('<%=Panel1.ClientID %>').scrollLeft;
                yPos = $get('<%=Panel1.ClientID %>').scrollTop;
            }
        }  
        function EndRequestHandler(sender, args){
            if ($get('<%= Panel1.ClientID %>') != null) {
                $get('<%= Panel1.ClientID %>').scrollLeft = xPos;
                $get('<%= Panel1.ClientID %>').scrollTop = yPos;
            }
        }
        prm.add_beginRequest(BeginRequestHandler);
        prm.add_endRequest(EndRequestHandler);
        function timeWarning(){
            document.getElementById('Label2').innerHTML = 'TEST';
            //alert('in timeWarning');
        }
    </script> 
	
	<script type="text/javascript" language="javascript">

    Sys.WebForms.PageRequestManager.getInstance().add_pageLoading(PageLoadingHandler);


    function PageLoadingHandler(sender, args) {

        var dataItems = args.get_dataItems();

        if (dataItems['Map1'] != null){
           processCallbackResult(dataItems['Map1'], 'Map1');
        }
    }

    </script>

    

    
      <esri:map id="Map1" runat="server" mapresourcemanager="MapResourceManager1"        
            style="z-index: 100; left: 268px; position: absolute; top: 309px; height: 446px; width: 497px; margin-bottom: 0px;" 
            BackColor="White" PrimaryMapResource="Regions">
          <esri:MapToolItem Name="MapToolItem0" />
        </esri:map>

        
        <input type="hidden" id="scrollPos" name="scrollPos" value="0" runat="server"/>



        <asp:UpdatePanel ID="UpdatePanel1" runat="server" UpdateMode="Conditional">

        <ContentTemplate>
        
       <asp:TreeView ID="TreeView2"
            onclick="javascript:postBackByObject(event)"
            runat="server" DataSourceID="XmlDataSource1" 
            ImageSet="BulletedList2" ExpandDepth="1" ShowCheckBoxes="Leaf" 
            style="overflow:auto; top: 23px; left: 446px; position: absolute; width: 262px; border-top-style: solid; height: 267px;" 
            ontreenodecheckchanged="TreeView2_CheckChanged" ShowLines="True">
            <ParentNodeStyle Font-Bold="False" />
            <HoverNodeStyle Font-Underline="True" ForeColor="#5555DD" />
            <SelectedNodeStyle Font-Underline="True" ForeColor="#5555DD" 
                HorizontalPadding="0px" VerticalPadding="0px" />
            <DataBindings>
                <asp:TreeNodeBinding DataMember="Dimension" ValueField="#Name" />
                <asp:TreeNodeBinding DataMember="Member" TextField="MemberName" />
            </DataBindings>
            <NodeStyle Font-Names="Verdana" Font-Size="8pt" ForeColor="Black" 
                HorizontalPadding="0px" NodeSpacing="0px" VerticalPadding="0px" />
        </asp:TreeView>    
            <asp:TextBox ID="TextBox1" runat="server" 
                style="top: 1px; left: 764px; position: absolute; height: 21px; width: 126px; margin-bottom: 8px;" 
                Visible="False" ReadOnly="True" BackColor="#BBBBBB" BorderStyle="None"></asp:TextBox>
            
        <asp:Panel 
            ID="Panel1" runat="server" 
            style="top: 30px; left: 765px; position: absolute; overflow:auto; height: 723px;" 
        Width="197px" ondisposed="Panel1_Disposed" >
           <asp:CheckBoxList ID="CheckBoxList1" runat="server" 
            DataSourceID="XmlDataSource2" 
            
            style="top: -4px; left: 0px; position: absolute; height: 140px; width: 122px;" 
            DataTextField="MemberName" Visible="False" AutoPostBack="True" 
            onselectedindexchanged="CheckBoxList1_SelectedIndexChanged" 
            onprerender="CheckBoxList1_PreRender">
            </asp:CheckBoxList>
        </asp:Panel>
        
        <asp:TextBox ID="TextBox2" runat="server" 
                style="top: 17px; left: 266px; position: absolute; height: 22px; width: 104px; background-color: #6699FF" 
                Visible="True" ReadOnly="True">Measures</asp:TextBox>
        <asp:DropDownList ID="DropDownList1" runat="server" 
                style="top: 52px; left: 266px; position: absolute; height: 22px; width: 110px; background-color: #6699FF" 
                DataSourceID="XmlDataSource3" DataTextField="MemberName" 
                AutoPostBack="True" onprerender="DropDownList1_PreRender"
                >
            </asp:DropDownList>
    
        
        
    
        
        </ContentTemplate>
            <Triggers>
                <asp:AsyncPostBackTrigger ControlID="CheckBoxList1" 
                    EventName="PreRender" />
            </Triggers>
        </asp:UpdatePanel>

   <asp:XmlDataSource 
            ID="XmlDataSource3" runat="server" DataFile="~/App_Code/App_Data/CBEO_MAP.xml" 
            XPath="//MeasuresLevel/Member"></asp:XmlDataSource>
    <asp:DropDownList ID="DropDownList3" runat="server" 
        
        
        
        style="top: 94px; left: 266px; position: absolute; height: 22px; width: 110px; background-color: #6699FF">
        <asp:ListItem>Quantile</asp:ListItem>
        <asp:ListItem>EqualInterval</asp:ListItem>
        <asp:ListItem>NaturalBreaks</asp:ListItem>
    </asp:DropDownList>
                
               
               
        <asp:DropDownList ID="DropDownList2" runat="server" 
        
        
        
        
        style="top: 203px; left: 266px; position: absolute; height: 22px; width: 110px; background-color: #6699FF">
            <asp:ListItem>3</asp:ListItem>
            <asp:ListItem>4</asp:ListItem>
            <asp:ListItem>5</asp:ListItem>
            <asp:ListItem>6</asp:ListItem>
            <asp:ListItem>7</asp:ListItem>
            <asp:ListItem>8</asp:ListItem>
            <asp:ListItem>9</asp:ListItem>
            <asp:ListItem>10</asp:ListItem>
    </asp:DropDownList>
    <asp:TextBox ID="TextBox4" runat="server" 
        
        style="top: 170px; left: 266px; position: absolute; height: 22px; width: 104px; background-color: #6699FF" 
        ReadOnly="True">Breaks</asp:TextBox>
       
&nbsp;&nbsp; &nbsp; &nbsp; &nbsp; &nbsp; &nbsp; &nbsp; &nbsp; &nbsp; &nbsp; &nbsp; 
        &nbsp; &nbsp; &nbsp; &nbsp; &nbsp; &nbsp; &nbsp; &nbsp; &nbsp; &nbsp; &nbsp; &nbsp;
&nbsp;&nbsp; &nbsp; &nbsp; &nbsp; &nbsp; &nbsp; &nbsp; &nbsp; &nbsp; &nbsp; &nbsp; &nbsp; &nbsp; &nbsp; &nbsp; &nbsp; &nbsp; &nbsp; &nbsp; &nbsp; &nbsp; &nbsp; &nbsp; &nbsp; &nbsp; &nbsp; &nbsp; &nbsp; 
        &nbsp; &nbsp; &nbsp; &nbsp; &nbsp; &nbsp; &nbsp; &nbsp; &nbsp; &nbsp; 
        
        &nbsp; &nbsp; &nbsp; &nbsp; &nbsp; &nbsp; &nbsp; &nbsp; &nbsp; &nbsp; &nbsp; &nbsp; &nbsp; &nbsp; &nbsp; &nbsp; &nbsp; &nbsp; &nbsp; &nbsp; &nbsp; &nbsp; &nbsp; &nbsp; &nbsp; &nbsp; &nbsp; &nbsp; &nbsp; &nbsp; 
        &nbsp;&nbsp;
      
            
   
                
       
    

                    
    
   
      
    
    
   
                
       
    

                    
    
   
      
      
            
   
                
       
    

                    
    
   
      
    
    
   
                
       
    

                    
    
   
        <esri:Toc ID="Toc1" runat="server" 
        
          
        
        
        style="top: 309px; left: 20px; position: absolute; height: 367px; width: 246px;" BuddyControl="Map1" 
        ExpandDepth="2" onnodechecked="Toc1_NodeChecked" />
    
    
   
                
       
    

                    
    
   
      
    
    
   
                
       
    

                    
    
   
      <esri:mapresourcemanager id="MapResourceManager1" runat="server" style="z-index: 101;
        left: -90px; position: absolute; top: 319px; right: 554px;"><ResourceItems>
              <esri:MapResourceItem Definition="&lt;Definition DataSourceDefinition=&quot;maxim&quot; DataSourceType=&quot;ArcGIS Server Local&quot; Identity=&quot;To set, right-click project and 'Add ArcGIS Identity'&quot; ResourceDefinition=&quot;Layers@CBEO&quot; DataSourceShared=&quot;True&quot; /&gt;" 
                  DisplaySettings="visible=True:transparency=0:mime=True:imgFormat=PNG8:height=100:width=100:dpi=96:color=-16711165:transbg=False:displayInToc=True:dynamicTiling=" 
                  LayerDefinitions="" Name="Regions" />
              <esri:MapResourceItem Definition="&lt;Definition DataSourceDefinition=&quot;http://maxim.ucsd.edu/arcgis/services&quot; DataSourceType=&quot;ArcGIS Server Internet&quot; Identity=&quot;&quot; ResourceDefinition=&quot;Layers@HISBaseMap&quot; DataSourceShared=&quot;True&quot; /&gt;" 
                  DisplaySettings="visible=True:transparency=0:mime=True:imgFormat=PNG8:height=100:width=100:dpi=96:color=:transbg=False:displayInToc=True:dynamicTiling=" 
                  LayerDefinitions="" Name="HISBasemaps" />
</ResourceItems>
</esri:mapresourcemanager>
    
    
   


        <asp:XmlDataSource ID="XmlDataSource1" runat="server" 
            DataFile="~/App_Code/App_Data/CBEO_MAP.xml" 
        
        TransformFile="~/App_Code/App_Data/DowntoAttributes.xsl" 
        XPath="//Dimensions"></asp:XmlDataSource>
    
    
   
                
       
    

                    
    
   
        <asp:UpdatePanel ID="UpdatePanel2" runat="server" UpdateMode="Conditional">
            <ContentTemplate>
                
                
                 <asp:CheckBox ID="CheckBox2" runat="server" Text="Animate" 
                     style=" position:absolute; top: 232px; left: 263px;" 
                     oncheckedchanged="AnimateCheckBox_CheckedChanged" />
                
                 <asp:HyperLink ID="HyperLink1" runat="server" 
                    style="position:absolute; top:232px; left:333px; height: 28px; width: 108px;" 
                    target="_blank"; ForeColor="#3399FF" Visible="False">Download Video</asp:HyperLink>
                
                 <asp:Button   ID="Button1" runat="server" OnClick="Button1_Click"    
                    Style="z-index: 103; left: 269px; position: absolute; top: 260px; 
                    border-style: solid; " Text="Generate" 
                    Width="83px" Font-Bold="False" />
                       
             <asp:CheckBox ID="CheckBox1"  style="position:absolute; top:25px; left:5px;"
             runat="server" Text="Debug" Checked="True" />
                 
             <asp:Panel ID="Panel3" runat="server"  
                 
                    
                    style="top: 43px; left: 13px; position: absolute; overflow:auto; height: 211px; width: 150px;">
        
                
                
                <asp:Label ID="Label2" runat="server" 
                 Style=" left: 7px; position: absolute;  width: 169px; top: 34px; height: 156px;" 
                    BorderColor="#666699" BorderWidth="0px"></asp:Label>
    
            
                 
            
    
            
                 
            
            </asp:Panel>
                
                 <asp:Button ID="Button2" runat="server" Font-Bold="false" 
                     OnClick="Button2_Click" 
                     style="left: 358px; position: absolute; top: 260px; border-style:solid" 
                     Text="Clear" Width="83px" />
            </ContentTemplate>
       
        </asp:UpdatePanel>
        
    
        
      <esri:toolbar id="Toolbar1" runat="server" buddycontroltype="Map" group="Toolbar1_Group"
        height="50px" style="z-index: 102; left: 265px; position: absolute; top: 770px; width: 498px; margin-top: 2px; height: 85px;"
        toolbaritemdefaultstyle-backcolor="#999999" toolbaritemdefaultstyle-font-names="Arial"
        toolbaritemdefaultstyle-font-size="Smaller" toolbaritemdisabledstyle-backcolor="White"
        toolbaritemdisabledstyle-font-names="Arial" toolbaritemdisabledstyle-font-size="Smaller"
        toolbaritemdisabledstyle-forecolor="Gray" toolbaritemhoverstyle-backcolor="#BBBBBB"
        toolbaritemhoverstyle-font-bold="True" toolbaritemhoverstyle-font-italic="True"
        toolbaritemhoverstyle-font-names="Arial" toolbaritemhoverstyle-font-size="Smaller"
        toolbaritemselectedstyle-backcolor="#BBBBBB" toolbaritemselectedstyle-font-bold="True"
        toolbaritemselectedstyle-font-names="Arial" toolbaritemselectedstyle-font-size="Smaller"
        webresourcelocation="/aspnet_client/ESRI/WebADF/" Width="400px" 
        oncommandclick="Toolbar1_CommandClick" ondisposed="Panel1_Disposed" ><ToolbarItems>
            <esri:Command ClientAction="" DefaultImage="esriFullExt.gif" HoverImage="esriFullExtU.gif"
                JavaScriptFile="" Name="MapFullExtent" SelectedImage="esriFullExtD.gif" ServerActionAssembly="ESRI.ArcGIS.ADF.Web.UI.WebControls"
                ServerActionClass="ESRI.ArcGIS.ADF.Web.UI.WebControls.Tools.MapFullExtent" Text="Full Extent"
                ToolTip="Full Extent" />
            <esri:Tool ClientAction="DragRectangle" DefaultImage="esriZoomIn.gif" HoverImage="esriZoomInU.gif"
                JavaScriptFile="" Name="MapZoomIn" SelectedImage="esriZoomInD.gif" ServerActionAssembly="ESRI.ArcGIS.ADF.Web.UI.WebControls"
                ServerActionClass="ESRI.ArcGIS.ADF.Web.UI.WebControls.Tools.MapZoomIn" Text="Zoom In"
                ToolTip="Zoom In" />
            <esri:Tool ClientAction="DragRectangle" DefaultImage="esriZoomOut.gif" HoverImage="esriZoomOutU.gif"
                JavaScriptFile="" Name="MapZoomOut" SelectedImage="esriZoomOutD.gif" ServerActionAssembly="ESRI.ArcGIS.ADF.Web.UI.WebControls"
                ServerActionClass="ESRI.ArcGIS.ADF.Web.UI.WebControls.Tools.MapZoomOut" Text="Zoom Out"
                ToolTip="Zoom Out" />
            <esri:Tool ClientAction="DragImage" DefaultImage="esriPan.gif" HoverImage="esriPanU.gif"
                JavaScriptFile="" Name="MapPan" SelectedImage="esriPanD.gif" ServerActionAssembly="ESRI.ArcGIS.ADF.Web.UI.WebControls"
                ServerActionClass="ESRI.ArcGIS.ADF.Web.UI.WebControls.Tools.MapPan" Text="Pan"
                ToolTip="Pan" />
              <esri:Tool ClientAction="Line" JavaScriptFile="" Name="Tool" 
                  ServerActionAssembly="App_Code" ServerActionClass="CrossSectionTool" 
                  Text="Cross Section" />
    </ToolbarItems>
    <BuddyControls>
    <esri:BuddyControl Name="Map1"></esri:BuddyControl>
    </BuddyControls>
    </esri:toolbar>

    
        <asp:XmlDataSource ID="XmlDataSource2" runat="server" 
            DataFile="~/App_Code/App_Data/CBEO_MAP.xml">
        </asp:XmlDataSource>
    
    
                <asp:Button ID="Button6" runat="server" Text="Setup" 
                style="left: 380px; top: 220; position:absolute; width: 51px" 
        onclick="Button6_Click" Visible="False"
                />
    <p>
        
        &nbsp;</p>
    
    
    </form>
        </body>
</html>
