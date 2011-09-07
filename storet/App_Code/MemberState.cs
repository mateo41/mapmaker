using System;
using System.Collections.Generic;

using System.IO;
using System.Xml;
using System.Xml.Xsl;
using System.Xml.XPath;
using System.Collections;
/// <summary>
/// Summary description for MemberState
/// Keeps track of the Members selected in the CheckListControl box
/// </summary>

/*An xml document describing the structure of the cube is generated
 * <Database>
 *      <Cube>
 *      <Dimensions>
 *      <Measures>
 * <Database>
 *
 * The dictionary keeps track of which attribute members are selected
 * The stack keeps track of the order of the attributes were selected to
 * update the TreeView
 */

public class MemberState
{
    private Dictionary<string, Dictionary<string, bool>> m_attrDict;
    private Stack m_attrStack;
    private String m_dataFile;
    private String m_measure;
    private String m_cube;
    private String m_database;
    private bool m_animate;
    private bool m_animate_attribute;

	public MemberState(String dataFile, String transformFile, String xpath)
	{
        m_dataFile = dataFile;
        m_attrDict = new Dictionary<string, Dictionary<string, bool>>();
        this.setupMemberState(transformFile, xpath);
        m_attrStack = new Stack();
	}
    public void setAnimate(bool animateValue){
        m_animate = animateValue;
    }

    public bool getAnimate(){
        return m_animate;
    }

    public void setAnimateAttribute(bool attrValue)
    {
        m_animate_attribute = attrValue;
    }

    public bool getAnimateAttribute()
    {
        return m_animate_attribute;
    }
    public void setCheckBoxState(string attr, string member, bool check){
        Dictionary<string, bool> d = m_attrDict[attr];
        d[member] = check;
    }

    public ArrayList getAttributeMembers(string attribute)
    {
        ArrayList attributeMembers = new ArrayList(m_attrDict[attribute].Keys);
        return attributeMembers;
    }
    public bool getCheckBoxState(string attr, string member)
    {
        Dictionary<string, bool> d = m_attrDict[attr];
        return d[member];
    }

    public void addCheckBoxState(string attr, string member)
    {
        if (!m_attrDict.ContainsKey(attr))
        {
            m_attrDict.Add(attr, new Dictionary<string, bool>());
        }

        m_attrDict[attr].Add(member, false);

    }

    public String getCurrentAttr(){
        return (String)m_attrStack.Peek();
    }

    public String popStack()
    {
        return (String)m_attrStack.Pop();
    }

    public void pushStack(String path)
    {
        m_attrStack.Push(path);
    }

    public bool emptyStack()
    {
        if (m_attrStack.Count == 0)
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    /*Returns the number of attributes that have selected members
     * in the m_attrDict
     * */
    private int getNumChecked()
    {
        int i = 0;
        foreach (string fullAttrPath in m_attrStack)
        {
            if (getMembersChecked(fullAttrPath) != 0)
            {
                i++;
            }
        }
        return i;
    }
    /* Arguments String fullpathAttr, the path attribute from the stack that
     * is used to access the key in the m_attrDict.
     * 
     * Return the number of members that are checked in the dictionary */
    private int getMembersChecked(String fullpathAttr)
    {
        Dictionary<string, bool> memberDict;
        int i = 0;
        foreach (string fullAttrPath in m_attrStack)
        {
            memberDict = m_attrDict["/" + fullAttrPath];
            
            foreach (string member in memberDict.Keys)
            {
                if (memberDict[member])
                {
                    i++;
                }

            }
        }
        return i;
    }

    /*Returns an array of strings in MDX syntax of the all of the 
     * attributes that have been checked.
     * 
     * [Dimension].[Attribute].&[Member]
     */ 
    public String[] getChecked()
    {
        
        String [] attrs = new String[getNumChecked()];
        Dictionary<string, bool> memberDict;
        String sep;
        int i = 0;

        foreach (string fullAttrPath in m_attrStack)
        {
            if (getMembersChecked(fullAttrPath) != 0)
            {
                String attr = fullAttrPath.Substring(fullAttrPath.IndexOf("/") + 1);
                string[] attrpath = attr.Split(new char[] { '/' });
                String mdx = "";
                sep = "";
                foreach (string a in attrpath)
                {
                    mdx = mdx + sep + "[" + a + "]";
                    sep = ".";

                }

                memberDict = m_attrDict["/" + fullAttrPath];
                String members = "";
                sep = "";

                foreach (string member in memberDict.Keys)
                {
                    if (memberDict[member])
                    {
                        members = members + sep + mdx + ".&[" + member + "]";
                        sep = ",";
                    }
                }
                attrs[i] = members;
                i++;
            }
        }
        return attrs;
    }

    public void removeFromStack(String valuePath)
    {
        Stack tmpStack = new Stack();
        while (valuePath != (String)m_attrStack.Peek())
        {
            tmpStack.Push(m_attrStack.Pop());
        }
        m_attrStack.Pop();
        while (tmpStack.Count > 0)
        {
            m_attrStack.Push(tmpStack.Pop());
        }
    }

    public void setupCubeString()
    {
        XmlDocument xmldoc = new XmlDocument();
        xmldoc.Load(System.Web.HttpContext.Current.Server.MapPath(m_dataFile));
        String xpath = "//Cubes";
        XmlNode cube = xmldoc.SelectSingleNode(xpath);
        XmlNode cubeNode = cube.FirstChild;
        m_cube =  cubeNode.Attributes["CubeName"].Value;
    }

    public String getCubeString()
    {
        return m_cube;
    }

    public void setupDatabaseString()
    {
        XmlDocument xmldoc = new XmlDocument();
        xmldoc.Load(System.Web.HttpContext.Current.Server.MapPath(m_dataFile));

        m_database = xmldoc.LastChild.Value;
        
    }

    public String getDatabaseString()
    {
        return m_database;
    }
    public void setMeasureString(String measure)
    {
        m_measure = measure;
    }

    public String getMeasureString()
    {
        return m_measure;
    }

    private void getLeaves(XmlNodeList nodes, string path, ArrayList attributes)
    {

        foreach (XmlNode node in nodes)
        {
            if (node.HasChildNodes)
            {
                string nodePath = path + "/" + node.Name;
                getLeaves(node.ChildNodes, nodePath, attributes);
            }
            else
            {
                string attrPath = path + "/" + node.Name;
                attributes.Add(attrPath);
            }
        }
    }

    /* This constructs the attribute dictionary from the xmldocument
     * The keys are the xpath to the attribute, the values are another 
     * dictionary of a string for each member and a bool to indicate whether
     * the member has been selected
     * 
     * To retrieve the attributes this first retrieves the Dimensions node
     * using the xpath query. For convenience it puts that node in its own
     * document. Then the document is transformed so all of the leaf elements which are
     * the members are excluded. The attributes are the leaf nodes of the transformed
     * xml document, they are retrieved and put into a list using the getLeaves method.
     * 
     * Each member node is the child node of the attribute. The memberName is an attribute
     * of the member node. Each memberName is added to the dictionary data structure
     * with the attribute name as the key.
     */ 
    private void setupMemberState( String transformFile, String xpath)
    {
        setupCubeString();
        setupDatabaseString();

        XmlDocument xmldoc = new XmlDocument();
        xmldoc.Load(System.Web.HttpContext.Current.Server.MapPath(m_dataFile));
        XmlNode dims = xmldoc.SelectSingleNode(xpath);

        XmlDocument xmlDimensions = new XmlDocument();
        xmlDimensions.LoadXml(dims.OuterXml);


        XslCompiledTransform transform = new XslCompiledTransform();
        transform.Load(System.Web.HttpContext.Current.Server.MapPath(transformFile));
        MemoryStream memStream = new MemoryStream();
        transform.Transform(xmlDimensions, (XsltArgumentList)null, (Stream)memStream);

        XmlDocument xmldocTransformed = new XmlDocument();
        memStream.Seek(0, System.IO.SeekOrigin.Begin);
        xmldocTransformed.Load(memStream);
        /*Retrieve all of the attributes in the transformed document */

        ArrayList attributes = new ArrayList();
        getLeaves(xmldocTransformed.ChildNodes, "", attributes);

        /*Now add all of the members to the dictionary associated with 
         * each attribute name.*/
        foreach (string path in attributes)
        {

            foreach (XmlNode node in xmlDimensions.SelectNodes(path))
            {
                foreach (XmlNode child in node.ChildNodes)
                {
                    foreach (XmlAttribute xmlAttr in child.Attributes)
                    {

                        this.addCheckBoxState(path, xmlAttr.Value);
                    }
                }
            }

        }

    }
}
