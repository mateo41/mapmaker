using System;
using System.Collections.Generic;


/// <summary>
/// Summary description for MDXQueryBuilder
/// </summary>
/// 
public class MDXQueryBuilder
{
    private Dictionary<int, string> m_layerDictionary;
    private string m_siteDim;

    public MDXQueryBuilder(Dictionary<int, string> layerDictionary, string siteDim)
    {
        m_layerDictionary = layerDictionary;
        m_siteDim = siteDim;
    }

    private String createSelectClause(int layerid, MemberState ms)
    {
        String selectQuery;
        String measure = ms.getMeasureString();
        selectQuery = "SELECT [Measures].["+ measure + "]  ON 0, ";
        String ret, spatialAttribute;
        spatialAttribute = "";
        String axis1 = " ON 1 ";
        spatialAttribute = m_layerDictionary[layerid];
      
        
        ret = selectQuery + m_siteDim + spatialAttribute + axis1;
        return ret;
    }


    /* Retrieves the cubeString from the MemberState, I should 
     * probably only do this once instead of everytime.
     */
    private String createFromClause(MemberState ms)
    {
        String cubeName = ms.getCubeString();
        return " FROM [" + cubeName + "] ";
    }

    private String createWhereClause(MemberState ms)
    {
        String[] attributes = ms.getChecked();
        String[] memberNames = createMemberNames(attributes);
        String sep = "";
        String memberNameList = "";
        String whereClause = "";
        if (memberNames.Length != 0)
        {
            whereClause = whereClause + " WHERE ( ";
            foreach (String memberName in memberNames)
            {
                memberNameList = memberNameList + sep + memberName;
                sep = ",";
            }
            whereClause = whereClause + memberNameList + ") ";
        }
        return whereClause;
    }

    public String buildQuery(int layerid, MemberState ms)
    {

        String selectClause, fromClause, whereClause, memberClause;

        String query;
        memberClause = createMembersClause(ms);
        selectClause = createSelectClause(layerid, ms);
        fromClause = createFromClause(ms);
        whereClause = createWhereClause(ms);

        query = memberClause + selectClause + fromClause + whereClause;
        return query;
    }

    public String createMembersClause(MemberState ms)
    {
        String[] attributes;

        attributes = ms.getChecked();
        String ret = "WITH ";
        int i = 0;
        String memberName, memberClause;
        foreach (String member in attributes)
        {
            memberName = createMemberName(member, i);
            memberClause = " MEMBER " + memberName + " AS ";
            ret = ret + memberClause + "'Aggregate({" + member + "})'  ";
            i++;
        }

        return ret;
    }


    private String createMemberName(String member, int i)
    {
        String attrStem;
        String memberName;
        attrStem = member.Substring(0, member.IndexOf("&"));
        memberName = attrStem + "[Q" + i.ToString() + "]";
        return memberName;
    }

    private String[] createMemberNames(String[] attributes)
    {
        String[] memberNames = new String[attributes.Length];
        String memberName;
        int i = 0;
        foreach (String member in attributes)
        {
            memberName = createMemberName(member, i);
            memberNames[i] = memberName;
            i++;
        }
        return memberNames;
    }

}

