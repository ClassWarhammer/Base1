﻿using System;
using System.Collections;
using System.Collections.Generic;
using Mono.Data.Sqlite;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

public class XQuickSetDatabase : XBaseWindow
{
    [UnityEditor.Callbacks.OnOpenAsset(2)]
    public static bool OnOpenAsset(int instanceID, int line)
    {
        var obj = EditorUtility.InstanceIDToObject(instanceID);
        string appDBPath = AssetDatabase.GetAssetPath(obj);
        if (obj != null && appDBPath.Contains(".db"))
        {
            InitWindow<XQuickSetDatabase>();
            dbFile = obj;
            return true;
        }
        return false;
    }


    static Object dbFile = null;
    List<string> allTable = new List<string>();

    List<string> allTableField = new List<string>();

    bool isShowTable = true;

    Dictionary<string, List<object>> tableDataDict = new Dictionary<string, List<object>>();
    Dictionary<string, Type> tableTypeDict = new Dictionary<string, Type>();
    string currTable = "";
    string filterStr = "";

    int dataTable = 0;

    int selectField = -1;

    private SqliteConnection dbConnection;
    private SqliteCommand dbCommand;
    private SqliteDataReader dataReader;

    private string sqlSentence = "";
    private string sqlSentenceResult = "";
    const string GET_ALL_TABLE_NAME = "select name from sqlite_master where type='table' order by name;";

    private GUILayoutOption option = GUILayout.Width( 150 );
    public override void OnXGUI()
    {
        //TODO List
        if (dbFile == null)
            Close();

        if (dbFile != null)
        {
            if (dbConnection == null)
            {
                string appDBPath = AssetDatabase.GetAssetPath(dbFile);
                try
                {
                    dbConnection = new SqliteConnection("URI=file:" + appDBPath);
                    dbConnection.Open();
                }
                catch (Exception e)
                {
                    XLogger.Log(e.Message);
                }

                currTable = "";
                GetAllTableName();
            }
            else
            {
                ReadAllTable();
            }
        }
    }
    public void GetAllTableName()
    {
        allTable.Clear();
        allTableField.Clear();
        SqliteDataReader recordTableReader = ExecuteQuery(GET_ALL_TABLE_NAME);
        while (recordTableReader.Read())
        {
            allTable.Add(recordTableReader.GetString(recordTableReader.GetOrdinal("name")));
        }
        dataTable = 0;
    }
    public void ReadAllTable()
    {
        if (CreateSpaceButton("DataBase", option))
        {
            GetAllTableName();
        }

        filterStr = CreateStringField( "Filter : ", filterStr );
        for (int pos = 0; pos < allTable.Count; pos++)
        {
            string tableName = allTable[pos];
            if( tableName.Contains( filterStr ) || filterStr.Contains( tableName ))
            {
                CreateTableButton( tableName );
            }
           
        }

        if (isShowTable)
        {
            ShowAllFields();
        }
    }

    public void CreateTableButton( string tableName )
    {
        if( CreateSpaceButton( tableName, option) )
        {
            dataTable = 0;
            allTableField.Clear();
            string sql = "SELECT * FROM " + tableName;
            SqliteDataReader recordTableReader = ExecuteQuery( sql );
            currTable = tableName;
            allTable.Clear();
            int count = recordTableReader.VisibleFieldCount;
            for( int idx = 0; idx < count; idx++ )
            {
                //  TODO loop in count
                string fieldTypeName = recordTableReader.GetName( idx );
            }

            selectField = -1;
            isShowTable = true;

            while( recordTableReader.Read() )
            {
                dataTable++;
                for( int root = 0; root < count; root++ )
                {
                    string field = recordTableReader.GetName( root );
                    Type type = recordTableReader.GetFieldType( root );
                    if( !allTableField.Contains( field ) )
                        allTableField.Add( field );
                    

                    if( !tableTypeDict.ContainsKey( field ) )
                        tableTypeDict.Add( field, type );
                    PushData( field, recordTableReader.GetValue( root ) );
                }
            }
            recordTableReader.Close();
        }
    }


    public void PushData(string fieldName, object o)
    {
        if (!tableDataDict.ContainsKey(fieldName))
            tableDataDict.Add(fieldName, new List<object>());
        var tableSet = tableDataDict[fieldName];
        tableSet.Add(o);

        

    }
    public void ReSelectField()
    {

    }

    public Type[] totalTypeContent = new Type[]
    {
        typeof(System.Boolean),
        typeof(System.Byte),
        typeof(System.SByte),
        typeof(System.Char),
        typeof(System.Char),
        typeof(System.Char),
        typeof(System.Decimal),
        typeof(System.Double),
        typeof(System.Single),
        typeof(System.Int32),
        typeof(System.UInt32),
        typeof(System.Int64),
        typeof(System.Int16),
        typeof(System.UInt16),
        typeof(System.String),
        typeof(DBNull),
    };
    public void ShowAllFields()
    {
        BeginHorizontal();
        for( int pos = 0; pos < allTableField.Count; pos++ )
        {
            tableTypeDict[allTableField[pos]] = 
            SelectableString<Type>( tableTypeDict[allTableField[pos]], totalTypeContent, (t)=>
            {

            }, option );
        }
        EndHorizontal();

        BeginHorizontal();

        for (int pos = 0; pos < allTableField.Count; pos++)
        {
            DoButton( allTableField[pos], () =>
           {
               selectField = pos;
               ReSelectField();
           }, option );

        }
        EndHorizontal();

        for (int root = 0; root < dataTable; root++)
        {
            BeginHorizontal();
            for (int pos = 0; pos < allTableField.Count; pos++)
            {
                var fieldName = allTableField[pos];

                var dataObject = tableDataDict[fieldName][root];

                if( selectField == pos )
                {
                    var dataText = dataObject.ToString();
                    if( !dataText.Contains( filterStr ) )
                    {
                        break;
                    }
                }

                if( tableDataDict[fieldName].Count > root && dataObject != null )
                {
                    tableDataDict[fieldName][root] = GetTypeGUI( dataObject, dataObject.GetType() );
                }
            }
            EndHorizontal();
        }

        BeginHorizontal();
        sqlSentence = CreateStringField( "Command Line:", sqlSentence );
        DoButton( "excute", () =>
         {
             sqlSentenceResult = "";
             var result = ExecuteQuery( sqlSentence );
             int index = 0;
             while( result.Read() )
             {

                 sqlSentenceResult += "\n" + result.GetValue( index ).ToString();
                 index++;
             }
         } );
        EndHorizontal();
        CreateTextArea( sqlSentenceResult );
    }


    protected object GetTypeGUI(object t, Type type)
    {
        if (t is int || t is System.Int32 || type == typeof(int))
        {
            t = CreateIntField(Convert.ToInt32(t), option);
        }
        else if (t is System.Int16)
        {
            t = (short)CreateIntField(Convert.ToInt16(t), option );
        }
        else if (t is System.Int64)
        {
            t = CreateLongField(Convert.ToInt64(t), option );
        }
        else if (t is byte)
        {
            int value = Convert.ToInt32(t);
            t = Convert.ToByte(CreateIntField(value, option) );
        }
        else if (type == typeof(String))
        {
            t = CreateStringField((string)t, option);
        }
        else if (type == typeof(Single) || type == typeof(Double))
        {
            t = CreateFloatField(Convert.ToSingle(t), option);
        }
        else if (type == typeof(Boolean))
        {
            bool v = Convert.ToBoolean( t );
            DoButton( v.ToString(), () =>
            {
                t = !v;
            }, option );
        }
        else if(type == typeof(DBNull))
        {
            CreateLabel("DBNull", false, option);
        }
        else if (type.BaseType == typeof(Enum))
        {
            t = CreateEnumSelectable("", (Enum)t ?? (Enum)Enum.ToObject(type, 0), option);
        }
        else if (type.IsSubclassOf(typeof(Object)))
        {
            t = CreateObjectField((Object)t, type, option);
        }
        else if (t is Vector2)
        {
            Vector2 v = (Vector2)t;
            t = CreateVector2Field(type.Name, v, option );
        }
        else if (t is Vector3)
        {
            Vector3 v = (Vector3)t;
            t = CreateVector3Field(type.Name, v, option );
        }
        else if (t is Vector4)
        {
            Vector4 v = (Vector4)t;
            t = CreateVector4Field(type.Name, v, option );
        }
        else if (t is Quaternion)
        {
            Quaternion q = (Quaternion)t;
            Vector4 v = new Vector4(q.x, q.y, q.z, q.w);
            v = CreateVector4Field(type.Name, v, option );
            q.x = v.x;
            q.y = v.y;
            q.z = v.z;
            q.w = v.w;
            t = q;
        }
        else if (typeof(IList).IsAssignableFrom(type))
        {
            IList list = t as IList;
            if (list == null)
                return t;
            BeginVertical();
            for (int pos = 0; pos < list.Count; pos++)
            {
                //  TODO loop in list.Count
                var o = list[pos];
                GetTypeGUI(o, o.GetType());
            }
            EndVertical();
        }
        else
        {
            CreateLabel(type.Name + " is not support", false, option );
        }

        return t;

    }

    void OnDisable()
    {
        Disconnect();
    }

    protected void Disconnect()
    {
        //销毁Command
        if( dbCommand != null )
        {
            dbCommand.Cancel();
        }
        dbCommand = null;

        //销毁Reader
        if( dataReader != null )
        {
            dataReader.Close();
        }
        dataReader = null;

        //销毁Connection
        if( dbConnection != null )
        {
            dbConnection.Close();
        }
        dbConnection = null;

    }

    public SqliteDataReader ExecuteQuery(string queryString)
    {
        dbCommand = dbConnection.CreateCommand();
        dbCommand.CommandText = queryString;
        dataReader = dbCommand.ExecuteReader();
        return dataReader;
    }
}
