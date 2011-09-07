using System;
using System.Data;
using System.Configuration;
using System.Linq;
using System.Web;
using System.Web.Security;
using System.Web.UI;
using System.Web.UI.HtmlControls;
using System.Web.UI.WebControls;
using System.Web.UI.WebControls.WebParts;
using System.Xml.Linq;
using System.Xml;
using System.Collections.Generic;
using System.Collections;
/// <summary>
/// Summary description for SitesListGenerator
/// </summary>
public class SitesListGenerator
{
    private XmlDocument m_xmldoc;
    public SitesListGenerator(string filename)
    {
        m_xmldoc = new XmlDocument();
        m_xmldoc.Load(filename);
    }
    public ArrayList getSitesList(String attr)
    {
        
        ArrayList sitesList = new ArrayList();
        
        String xpath = "//" + attr;
        XmlNode attribute = m_xmldoc.SelectSingleNode(xpath);
        foreach (XmlNode node in attribute.ChildNodes)
        {
            foreach (XmlAttribute xmlAttr in node.Attributes)
            {
                sitesList.Add(xmlAttr.Value);
            }
        }
        return sitesList;
    }
	

}
