/*---------------------------------------------------------------------------------------------
 *  Copyright (c) by Solusek Systems. All rights reserved.
 *--------------------------------------------------------------------------------------------*/


using System.Collections.Concurrent;
using Microsoft.Data.SqlClient;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Ubriaco.Attributes;

namespace Ubriaco.Models;

// M to avoid confusion
public class MSQLBaseModel : RootModel
{
    // Reflection metadata is shared across instances; legacy column mapping remains instance-specific.
    private static readonly ConcurrentDictionary<Type, PropertyInfo[]> PropertyCache = new();

    private PropertyInfo[] GetCachedProperties() =>
        PropertyCache.GetOrAdd(GetType(), static type => type.GetProperties());

    public static DateTime MinDT = DateTime.Parse("1753-1-1"); // this has something to do with SQL

    public MSQLBaseModel()
    {
    }

    public MSQLBaseModel(DbDataReader r)
    {
        Populate(r);
    }

    public virtual Dictionary<string, string> Validate()
    {
        return new Dictionary<string, string>();
    }

    public virtual bool PreWrite()
    {
        return true;
    }

    public virtual bool InUse(SQLBaseConnection db)
    {
        return true;
    }


    public virtual void PreInsert(SqlConnection db) { }
    public virtual void PreUpdate(SqlConnection db) { }
    public virtual void PreDelete(SqlConnection db) { }
    public virtual void ParseFromString(string s) { throw new Exception("ParseString: Not implemented."); }
    public virtual string SerializeToString() { return null; }


    protected DateOnly? QGetDateOnly(DbDataReader r, string n)
    {
        try
        {
            var str = QGetString(r, n);
            if (str.Contains(' '))
            {
                var p = str.Split(' ');
                return DateOnly.Parse(p[0]);
            }
            else
                return DateOnly.Parse(str);
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
        }
        return null;
    }


    protected string QGetString(DbDataReader r, string n)
    {
        try
        {
            if (r[n] != null)
                return r[n].ToString();
        }
        catch (Exception e)
        {
        }
        return null;
    }


    protected int QGetInt(DbDataReader r, string n)
    {
        try
        {
            if (r[n] != null)
                return Convert.ToInt32(r[n]);
        }
        catch (Exception e)
        {
        }
        return 0;
    }

    protected short QGetShort(DbDataReader r, string n)
    {
        try
        {
            if (r[n] != null)
                return Convert.ToInt16(r[n]);
        }
        catch (Exception e)
        {
        }
        return 0;
    }

    protected long QGetLong(DbDataReader r, string n)
    {
        try
        {
            if (r[n] != null)
                return Convert.ToInt64(r[n]);
        }
        catch (Exception e)
        {
        }
        return 0;
    }

    public virtual void CopyFrom(MSQLBaseModel other)
    {
        PropertyInfo[] fields = GetCachedProperties();
        foreach (PropertyInfo f in fields)
        {
            try
            {
                f.SetValue(this, f.GetValue(other));
            }
            catch { }
        }
    }

    public virtual List<Diff> Compare(MSQLBaseModel other, bool ignoreNulls = false, bool ignoreBlanks = false)
    {
        List<Diff> list = new List<Diff>();
        PropertyInfo[] fields = GetCachedProperties();
        foreach (PropertyInfo f in fields)
        {
            if (!IsDataField(f) || f.Name == "__MDT" || f.Name == "CreatedDate" || f.Name == "MasterID" || f.Name == "IsSync" || f.Name == "HNSync")
                continue;
            string vname = GetRealDataFieldName(f);

            Diff diff = new Diff();
            diff.ModelName = this.GetType().Name;
            diff.Name = vname;
            diff.Current = f.GetValue(this);
            diff.Other = f.GetValue(other);
            if (ignoreNulls && diff.Other == null)
                continue;
            diff.Type = null;
            if (f.PropertyType == typeof(string))
            {
                if (ignoreBlanks && diff.Other != null && ((string)diff.Other).Length == 0)
                    continue;
                diff.Type = "STRING";
                if ((string)diff.Current != (string)diff.Other)
                    list.Add(diff);
            }
            else if (f.PropertyType == typeof(int))
            {
                diff.Type = "INT";
                if ((int)diff.Current != (int)diff.Other)
                    list.Add(diff);
            }
            else if (f.PropertyType == typeof(short))
            {
                diff.Type = "SMALLINT";
                if ((short)diff.Current != (short)diff.Other)
                    list.Add(diff);
            }
            else if (f.PropertyType == typeof(double))
            {
                diff.Type = "DOUBLE";
                if ((double)diff.Current != (double)diff.Other)
                    list.Add(diff);
            }
            else if (f.PropertyType == typeof(float))
            {
                diff.Type = "FLOAT";
                if ((float)diff.Current != (float)diff.Other)
                    list.Add(diff);
            }
            else if (f.PropertyType == typeof(bool))
            {
                diff.Type = "BOOL";
                if ((bool)diff.Current != (bool)diff.Other)
                    list.Add(diff);
            }
            else if (f.PropertyType == typeof(DateTime))
            {
                diff.Type = "DATETIME";
                if ((DateTime)diff.Current != (DateTime)diff.Other)
                    list.Add(diff);
            }


        }
        return list;
    }

    private bool HasColumn(IDataRecord dr, string columnName)
    {
        for (int i = 0; i < dr.FieldCount; i++)
        {
            if (dr.GetName(i).Equals(columnName, StringComparison.InvariantCultureIgnoreCase))
                return true;
        }
        return false;
    }


    // Performance optimization required here
    public virtual void Populate(DbDataReader r)
    {
        PropertyInfo[] fields = GetCachedProperties();
        var ordinals = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        for (int i = 0; i < r.FieldCount; i++)
            ordinals.TryAdd(r.GetName(i), i);
        foreach (PropertyInfo f in fields)
        {
            if (!f.CanWrite) continue;

            string vname = GetRealDataFieldName(f);

            try
            {
                if (!ordinals.TryGetValue(vname, out int ordinal))
                    continue;
                if (r.IsDBNull(ordinal))
                {
                    f.SetValue(this, f.PropertyType.IsValueType ? Activator.CreateInstance(f.PropertyType) : null);
                    continue;
                }

                if (f.PropertyType == typeof(int))
                    f.SetValue(this, QGetInt(r, vname));
                else if (f.PropertyType == typeof(short))
                    f.SetValue(this, QGetShort(r, vname));
                else if (f.PropertyType == typeof(long))
                    f.SetValue(this, QGetLong(r, vname));
                else if (f.PropertyType == typeof(double))
                    f.SetValue(this, Convert.ToDouble(r[vname]));
                else if (f.PropertyType == typeof(decimal))
                    f.SetValue(this, Convert.ToDecimal(r[vname]));
                else if (f.PropertyType == typeof(float))
                    f.SetValue(this, (float)Convert.ToDouble(r[vname]));
                else if (f.PropertyType == typeof(string))
                    f.SetValue(this, QGetString(r, vname));
                else if (f.PropertyType == typeof(DateOnly?) && r[vname] != DBNull.Value)
                    f.SetValue(this, QGetDateOnly(r, vname));
                else if (f.PropertyType == typeof(DateTime?) && r[vname] != DBNull.Value)
                    f.SetValue(this, DateTime.Parse(QGetString(r, vname)));
                else if (f.PropertyType == typeof(DateOnly) && r[vname] != DBNull.Value)
                    f.SetValue(this, QGetDateOnly(r, vname));
                else if (f.PropertyType == typeof(DateTime) && r[vname] != DBNull.Value)
                    f.SetValue(this, DateTime.Parse(QGetString(r, vname)));
                else if (f.PropertyType == typeof(bool))
                    f.SetValue(this, r[vname] == DBNull.Value ? false : (QGetInt(r, vname) == 0 ? false : true));
                else
                {
                    f.SetValue(this, r[vname] == DBNull.Value ? null : JsonConvert.DeserializeObject(r[vname].ToString(), f.PropertyType));
                }
            }
            catch (Exception e)
            {
                if (vname == "ID")
                    Debug.WriteLine("Table has no ID.");
                else
                    Debug.WriteLine("For: " + vname + " " + e.StackTrace);
            }
        }


    }

    public virtual void PopulateFromDynamic(dynamic o)
    {
        PropertyInfo[] fields = GetCachedProperties();
        foreach (PropertyInfo f in fields)
        {
            if (!f.CanWrite || f.Name == "MasterID")
                continue;

            string vname = GetRealDataFieldName(f);

            try
            {
                dynamic i = o[vname];

                if (f.PropertyType == typeof(int))
                    f.SetValue(this, Convert.ToInt32(i));
                else if (f.PropertyType == typeof(double))
                    f.SetValue(this, Convert.ToDouble(i));
                else if (f.PropertyType == typeof(string))
                    f.SetValue(this, Convert.ToString(i));
                else if (f.PropertyType == typeof(DateTime))
                    f.SetValue(this, DateTime.Parse(Convert.ToString(i)));
                else if (f.PropertyType == typeof(bool))
                    f.SetValue(this, Convert.ToBoolean(i));
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.StackTrace);
            }
        }
    }

    public virtual bool SetProp(string propName, dynamic value)
    {
        PropertyInfo[] fields = GetCachedProperties();
        foreach (PropertyInfo f in fields)
        {
            if (f.Name == propName)
            {
                if (f.PropertyType == typeof(int))
                    f.SetValue(this, Convert.ToInt32(value));
                else if (f.PropertyType == typeof(long))
                    f.SetValue(this, Convert.ToInt64(value));
                else
                    f.SetValue(this, value);
                return true;
            }
        }
        return false;
    }


    public virtual void PopulateFromJson(dynamic o, bool onlyEditable = false)
    {
        if (o == null)
            return;
        PropertyInfo[] fields = GetCachedProperties();
        foreach (PropertyInfo f in fields)
        {
            //if (f.Name[0] == '_')
            //   continue;
            if (f.Name == "MasterID")
                continue;

            if (onlyEditable)
            {
                Attribute at = f.GetCustomAttribute(typeof(ColVisible));
                if (at != null)
                {
                    ColVisible cv = (ColVisible)at;
                    if (!cv.Editable)
                        continue;
                }
                else
                    continue;
            }

            /*
            Attribute at = f.GetCustomAttribute(typeof(ColVisible));

            if (at == null)
                continue;

            ColVisible cv = (ColVisible)at;


            if (cv.ColType == 1)
            {
                if (o[f.Name] != null)
                {
                    try
                    {
                        string s = Convert.ToString(o[f.Name]);
                        if (s.Length > 0)
                        {
                            SHA1Managed sham = new SHA1Managed();
                            string res = Convert.ToBase64String(sham.ComputeHash(Encoding.ASCII.GetBytes(s)));
                            f.SetValue(this, res);
                        }
                    }
                    catch (InvalidOperationException e)
                    {

                    }
                }
                continue;
            }
            else */
            try
            {
                if (f.PropertyType == typeof(int) && o[f.Name] != null)
                    f.SetValue(this, Convert.ToInt32(o[f.Name]));
                else if (f.PropertyType == typeof(string) && o[f.Name] != null)
                    f.SetValue(this, Convert.ToString(o[f.Name]));
                else if (f.PropertyType == typeof(DateTime) && o[f.Name] != null && Convert.ToString(o[f.Name]).Length > 0)
                    f.SetValue(this, DateTime.Parse(Convert.ToString(o[f.Name])));
                else if (f.PropertyType == typeof(bool) && o[f.Name] != null)
                    f.SetValue(this, Convert.ToBoolean(o[f.Name]));
                else if ((f.PropertyType == typeof(double) || f.PropertyType == typeof(float)) && o[f.Name] != null)
                    f.SetValue(this, Convert.ToDouble(o[f.Name]));
                else if (f.PropertyType == typeof(decimal) && o[f.Name] != null)
                    f.SetValue(this, Convert.ToDecimal(o[f.Name]));
                else
                {
                    f.SetValue(this, JsonConvert.DeserializeObject(JsonConvert.SerializeObject(o[f.Name]), f.PropertyType));
                }
            }
            catch (Exception e)
            {

            }
            //else if (o[f.Name] != null)
            //   f.SetValue(this, o[f.Name]);
        }
    }


    public virtual void _PopulateFromJsonSpecial(dynamic o, bool onlyEditable = false)
    {
        if (o == null)
            return;

        Dictionary<string, object> dict = new Dictionary<string, object>();
        try
        {
            foreach (JProperty i in o)
            {
                dict[i.Name] = i.Value;
            }
        }
        catch (Exception e)
        {
            return;
        }
        PropertyInfo[] fields = GetCachedProperties();
        foreach (PropertyInfo f in fields)
        {
            if (onlyEditable)
            {
                Attribute at = f.GetCustomAttribute(typeof(ColVisible));
                if (at != null)
                {
                    ColVisible cv = (ColVisible)at;
                    if (!cv.Editable)
                        continue;
                }
                else
                    continue;
            }

            try
            {
                if (f.PropertyType == typeof(int) && dict[f.Name] != null)
                    f.SetValue(this, Convert.ToInt32(dict[f.Name]));
                else if (f.PropertyType == typeof(string) && dict[f.Name] != null)
                    f.SetValue(this, Convert.ToString(dict[f.Name]));
                else if (f.PropertyType == typeof(DateTime) && dict[f.Name] != null && Convert.ToString(dict[f.Name]).Length > 0)
                    f.SetValue(this, DateTime.Parse(Convert.ToString(dict[f.Name])));
                else if (f.PropertyType == typeof(bool) && dict[f.Name] != null)
                    f.SetValue(this, Convert.ToBoolean(dict[f.Name]));
                else if ((f.PropertyType == typeof(double) || f.PropertyType == typeof(float)) && dict[f.Name] != null)
                    f.SetValue(this, Convert.ToDouble(dict[f.Name]));
            }
            catch (Exception e)
            {

            }
        }
    }

    public virtual void PopulateFromJsonSpecial(dynamic o, bool onlyEditable = false)
    {
        if (o == null)
            return;

        Dictionary<string, object> dict = new Dictionary<string, object>();
        try
        {
            foreach (KeyValuePair<string, object> i in o)
            {
                dict[i.Key] = i.Value;
            }
        }
        catch (Exception e)
        {
            return;
        }
        PropertyInfo[] fields = GetCachedProperties();
        foreach (PropertyInfo f in fields)
        {
            if (f.Name == "MasterID")
                continue;
            if (onlyEditable)
            {
                Attribute at = f.GetCustomAttribute(typeof(ColVisible));
                if (at != null)
                {
                    ColVisible cv = (ColVisible)at;
                    if (!cv.Editable)
                        continue;
                }
                else
                    continue;
            }

            try
            {
                if (f.PropertyType == typeof(int) && dict[f.Name] != null)
                    f.SetValue(this, Convert.ToInt32(dict[f.Name]));
                else if (f.PropertyType == typeof(string) && dict[f.Name] != null)
                    f.SetValue(this, Convert.ToString(dict[f.Name]));
                else if (f.PropertyType == typeof(DateTime) && dict[f.Name] != null && Convert.ToString(dict[f.Name]).Length > 0)
                    f.SetValue(this, DateTime.Parse(Convert.ToString(dict[f.Name])));
                else if (f.PropertyType == typeof(bool) && dict[f.Name] != null)
                    f.SetValue(this, Convert.ToBoolean(dict[f.Name]));
                else if ((f.PropertyType == typeof(double) || f.PropertyType == typeof(float)) && dict[f.Name] != null)
                    f.SetValue(this, Convert.ToDouble(dict[f.Name]));
            }
            catch (Exception e)
            {

            }
        }
    }


    public virtual void PutParams(SqlCommand cmd)
    {
        PropertyInfo[] fields = GetCachedProperties();
        foreach (PropertyInfo f in fields)
        {
            if (!IsDataField(f))
                continue;


            if (f.GetValue(this) == null)
                cmd.Parameters.AddWithValue(f.Name, DBNull.Value);
            else if (f.PropertyType == typeof(DateOnly))
            {
                var dt = (DateOnly)f.GetValue(this);
                cmd.Parameters.AddWithValue(f.Name, dt.ToString("yyyy-MM-dd"));
            }
            else if (f.PropertyType == typeof(DateTime?))
            {
                var dt = (DateTime?)f.GetValue(this);
                if (dt != null)
                    cmd.Parameters.AddWithValue(f.Name, dt.Value);
                else
                    cmd.Parameters.AddWithValue(f.Name, DBNull.Value);
            }
            else if (f.PropertyType == typeof(DateTime))
            {
                DateTime dt = (DateTime)f.GetValue(this);
                if (dt <= MinDT)
                    cmd.Parameters.AddWithValue(f.Name, DBNull.Value);
                else
                {
                    cmd.Parameters.AddWithValue(f.Name, dt);
                }
            }
            /*
            else if (f.PropertyType.IsSubclassOf(typeof(BaseModel)))
            {
                BaseModel m = (BaseModel)f.GetValue(this);
                //if (m == null)
                //    cmd.Parameters.AddWithValue(f.Name, 0);
                //else
                cmd.Parameters.AddWithValue(f.Name, m.ID);
            }*/
            else if (f.PropertyType == typeof(int) || f.PropertyType == typeof(long) || f.PropertyType == typeof(short) || f.PropertyType == typeof(string) || f.PropertyType == typeof(double) || f.PropertyType == typeof(float) || f.PropertyType == typeof(decimal))
                cmd.Parameters.AddWithValue(f.Name, f.GetValue(this));
            else if (f.PropertyType == typeof(bool))
                cmd.Parameters.AddWithValue(f.Name, (bool)f.GetValue(this) ? 1 : 0);
            else
            {
                try
                {
                    cmd.Parameters.AddWithValue(f.Name, JsonConvert.SerializeObject(f.GetValue(this)));
                }
                catch { }
            }
        }
    }


    public virtual async Task<int> TryInsertAsync(SqlConnection c)
    {
        try
        {
            return await InsertAsync(c);
        }
        catch { }
        return 0;
    }


    public virtual async Task<int> InsertAsync(SqlConnection c, string tableName = null, bool identityInsert = false)
    {
        PreInsert(c);
        if (tableName == null) tableName = GetDefaultTable();
        string qvars = "", vvars = "";
        PropertyInfo[] fields = GetCachedProperties();
        foreach (PropertyInfo f in fields)
        {
            if ((IsIncrement(f) && !identityInsert) || !IsDataField(f))
                continue;
            if (qvars.Length > 0)
            {
                qvars += ",";
                vvars += ",";
            }
            qvars += GetRealDataFieldName(f);
            vvars += "@" + f.Name;
        }
        using (SqlCommand cmd = new SqlCommand("INSERT INTO " + tableName + " (" + qvars + ") VALUES (" + vvars + "); SELECT SCOPE_IDENTITY()", c))
        {
            PutParams(cmd);
            var r = await cmd.ExecuteScalarAsync();
            return (r == DBNull.Value ? 0 : Convert.ToInt32(r));
        }
    }


    public virtual async Task<int> InsertAsync(DbConnection c, string tableName = null, bool identityInsert = false)
    {
        if (c.GetType() == typeof(SqlConnection))
            return await InsertAsync((SqlConnection)c, tableName, identityInsert);
        throw new Exception("No DB type supported.");
    }

    public virtual async Task<bool> InsertNonQueryAsync(SqlConnection c, string tableName = null)
    {
        if (tableName == null) tableName = GetDefaultTable();
        string qvars = "", vvars = "";
        PropertyInfo[] fields = GetCachedProperties();
        foreach (PropertyInfo f in fields)
        {
            if (f.Name == "ID" || !IsDataField(f))
                continue;
            if (qvars.Length > 0)
            {
                qvars += ",";
                vvars += ",";
            }
            qvars += GetRealDataFieldName(f);
            vvars += "@" + f.Name;
        }
        try
        {
            using (SqlCommand cmd = new SqlCommand("INSERT INTO " + tableName + " (" + qvars + ") VALUES (" + vvars + "); SELECT SCOPE_IDENTITY()", c))
            {
                PutParams(cmd);
                await cmd.ExecuteNonQueryAsync();
            }
            return true;
        }
        catch (Exception e)
        {
            return false;
        }
    }






    public virtual async Task<bool> DeleteFromNNAsync(SqlConnection c, string tableName = null)
    {
        PreDelete(c);
        if (tableName == null) tableName = GetDefaultTable();
        string qvars = "";
        PropertyInfo[] fields = GetCachedProperties();
        foreach (PropertyInfo f in fields)
        {
            if (f.Name == "ID" || !IsDataField(f))
                continue;
            if (qvars.Length > 0)
                qvars += " AND ";
            qvars += GetRealDataFieldName(f) + "=@" + f.Name;
        }
        if (qvars.Length == 0)
            throw new InvalidOperationException("Delete requires at least one predicate.");
        try
        {
            using (SqlCommand cmd = new SqlCommand("DELETE FROM " + tableName + " WHERE " + qvars, c))
            {
                PutParams(cmd);
                await cmd.ExecuteNonQueryAsync();
            }
            return true;
        }
        catch (Exception e)
        {
            return false;
        }
    }

    public virtual async Task UpdateAsync(DbConnection c, string tableName = null, string idName = "ID")
    {
        await UpdateAsync(c, tableName, new List<string>());
    }

    public virtual async Task UpdateAsync(DbConnection c, string tableName, List<string> exclude)
    {
        if (tableName == null) tableName = GetDefaultTable();
        var qvars = new List<string>();
        var idList = new List<string>();
        PropertyInfo[] fields = GetCachedProperties();
        foreach (PropertyInfo f in fields)
        {
            if (!IsDataField(f))
                continue;
            if (IsIdentity(f))
            {
                idList.Add(GetRealDataFieldName(f) + "=@" + f.Name);
                continue;
            }
            if (exclude != null && exclude.Contains(f.Name))
                continue;
            qvars.Add(GetRealDataFieldName(f) + "=@" + f.Name);

        }

        if (idList.Count == 0)
            throw new InvalidOperationException("Update requires at least one identity field.");
        if (qvars.Count == 0)
            throw new InvalidOperationException("Update requires at least one data field.");
        DbCommand cmd;
        if (c.GetType() == typeof(SqlConnection))
        {
            cmd = new SqlCommand("UPDATE " + tableName + " SET " + string.Join(", ", qvars) + " WHERE " + string.Join(" AND ", idList), (SqlConnection)c);
            PutParams((SqlCommand)cmd);
        }
        else
        {
            throw new Exception("No DB type supported.");
        }
        await using (cmd)
        {
            await cmd.ExecuteNonQueryAsync();
        }
    }

    public virtual async Task Update2Async(SQLBaseConnection c, string q, string tableName = null, string idName = "ID")
    {
        string[] qq;
        if (q.Contains(','))
            qq = q.Split(',');
        else
            qq = new string[] { q };

        await UpdateAsync(c.Connection, new List<string>(qq), tableName, idName, null);
    }

    public virtual async Task UpdateAsync(DbConnection c, List<string> q, string tableName = null, string idName = "ID")
    {
        await UpdateAsync(c, q, tableName, idName, null);
    }

    public virtual async Task UpdateAsync(DbConnection c, List<string> q, string tableName, string idName, List<string> exclude)
    {
        PreUpdate((SqlConnection)c);
        if (tableName == null) tableName = GetDefaultTable();
        var qvars = new List<string>();
        var idList = new List<string>();
        PropertyInfo[] fields = GetCachedProperties();
        foreach (PropertyInfo f in fields)
        {
            if (!IsDataField(f))
                continue;
            if (IsIdentity(f))
            {
                idList.Add(GetRealDataFieldName(f) + "=@" + f.Name);
                continue;
            }
            if (q != null && !q.Contains(f.Name))
                continue;
            if (exclude != null && exclude.Contains(f.Name))
                continue;
            qvars.Add(GetRealDataFieldName(f) + "=@" + f.Name);

        }
        if (idList.Count == 0)
            throw new InvalidOperationException("Update requires at least one identity field.");
        if (qvars.Count == 0)
            throw new InvalidOperationException("Update requires at least one selected data field.");

        DbCommand cmd;
        if (c.GetType() == typeof(SqlConnection))
        {
            cmd = new SqlCommand("UPDATE " + tableName + " SET " + string.Join(", ", qvars) + " WHERE " + string.Join(" AND ", idList), (SqlConnection)c);
            PutParams((SqlCommand)cmd);
        }
        else
        {
            throw new Exception("No DB type supported.");
        }
        await using (cmd)
        {
            await cmd.ExecuteNonQueryAsync();
        }
    }



    public virtual async Task<bool> Select2Async(SQLBaseConnection c, string q, string tableName = null, string orderBy = null)
    {
        string[] qq;
        if (q.Contains(','))
            qq = q.Split(',');
        else
            qq = new string[] { q };
        return await SelectAsync(c.Connection, new List<string>(qq), tableName, orderBy);
    }

    public virtual async Task<bool> SelectAsync(DbConnection c, List<string> q, string? tableName = null, string? orderBy = null)
    {
        if (tableName == null) tableName = GetDefaultTable();
        PropertyInfo[] fields = GetCachedProperties();
        string qvars = "";
        foreach (PropertyInfo f in fields)
        {
            if (!q.Contains(f.Name))
                continue;
            if (qvars.Length > 0)
            {
                qvars += " AND ";
            }
            qvars += GetRealDataFieldName(f) + "=@" + f.Name;
        }
        DbCommand cmd;
        if (c.GetType() == typeof(SqlConnection))
        {
            cmd = new SqlCommand("SELECT TOP 1 * FROM " + tableName + " WHERE " + qvars + (orderBy != null ? " ORDER BY " + orderBy : ""), (SqlConnection)c);
            PutParams((SqlCommand)cmd);
        }
        else
        {
            throw new Exception("No DB type supported.");
        }
        await using (cmd)
        {
            using (DbDataReader r = await cmd.ExecuteReaderAsync())
            {
                if (await r.ReadAsync())
                {
                    Populate(r);
                    return true;
                }
            }
            return false;
        }
    }

    public virtual async Task<List<T>> List2Async<T>(SQLBaseConnection c, string q, string tableName = null, string sort = null, int limit = 0) where T : MSQLBaseModel
    {
        string[] qq;
        if (q.Contains(','))
            qq = q.Split(',');
        else
            qq = new string[] { q };



        return await ListAsync<T>(c.Connection, new List<string>(qq), tableName, sort, limit);
    }

    public virtual async Task<List<T>> ListAsync<T>(DbConnection c, List<string> q, string tableName = null, string sort = null, int limit = 0) where T : MSQLBaseModel
    {
        List<T> list = new List<T>();
        if (tableName == null) tableName = GetDefaultTable();
        PropertyInfo[] fields = GetCachedProperties();
        string qvars = "";
        if (q != null)
        {
            foreach (PropertyInfo f in fields)
            {
                if (!q.Contains(f.Name))
                    continue;
                if (qvars.Length > 0)
                {
                    qvars += " AND ";
                }
                qvars += GetRealDataFieldName(f) + "=@" + f.Name;
            }
        }
        DbCommand cmd;
        if (c.GetType() == typeof(SqlConnection))
        {
            cmd = new SqlCommand("SELECT " + (limit != 0 ? " TOP " + limit : "") + " * FROM " + tableName + (qvars.Length > 0 ? " WHERE " + qvars : "") + (sort != null ? " ORDER BY " + sort : ""), (SqlConnection)c);
            PutParams((SqlCommand)cmd);
        }
        else
        {
            throw new Exception("No DB type supported.");
        }
        await using (cmd)
        {
            using (DbDataReader r = await cmd.ExecuteReaderAsync())
            {
                while (await r.ReadAsync())
                {
                    T item = (T)Activator.CreateInstance(typeof(T))!;
                    item.Populate(r);
                    list.Add(item);
                }
            }
            return list;
        }
    }

    public virtual void CreateFKeys(SqlConnection c)
    {
        string tableName = GetDefaultTable();
        PropertyInfo[] fields = GetCachedProperties();

        foreach (var f in fields)
        {
            if (IsModelLocal(f))
                continue;
            var fk = f.GetCustomAttribute<FKey>();
            if (fk == null)
                continue;

            string constraintName = "FK_" + tableName + "_" + f.Name;

            var refModel = (MSQLBaseModel?)Activator.CreateInstance(fk.ModelType);

            if (refModel == null)
                throw new Exception("Ref model does not exist.");

            using (var cmd = new SqlCommand(@$"ALTER TABLE {tableName}
                                                ADD CONSTRAINT {constraintName}
                                                FOREIGN KEY ({f.Name})
                                                REFERENCES {refModel.GetDefaultTable()}({fk.PropertyName})", c))
            {
                try
                {
                    cmd.ExecuteNonQuery();
                }
                catch { }
            }
        }
    }

    public virtual bool CreateTable(DbConnection c, bool includeExternalID = false)
    {
        string tableName = GetDefaultTable();
        PropertyInfo[] fields = GetCachedProperties();

        var keyList = new List<string>();
        string qvars = "";
        foreach (PropertyInfo f in fields)
        {
            if (IsModelLocal(f))
                continue;


            if (qvars.Length > 0)
            {
                qvars += ", ";
            }

            if (c.GetType() == typeof(SqlConnection))
                qvars += GetRealDataFieldName(f) + " ";
            else
                throw new Exception("No DB type supported.");

            if (f.PropertyType == typeof(int))
                qvars += "INT";
            else if (f.PropertyType == typeof(long))
                qvars += "BIGINT";
            else if (f.PropertyType == typeof(short))
                qvars += "SMALLINT";
            else if (f.PropertyType == typeof(string))
            {
                if (GetVarTypeInfo(f).ToUpper() == "TEXT")
                    qvars += "TEXT";
                else if (GetVarTypeInfo(f).ToUpper().Contains("VARCHAR"))
                    qvars += GetVarTypeInfo(f).ToUpper();
                else
                    qvars += "VARCHAR(256)";
            }
            else if (f.PropertyType == typeof(float) || f.PropertyType == typeof(double))
                qvars += "FLOAT";
            else if (f.PropertyType == typeof(decimal))
                qvars += "DECIMAL(10,2)";
            else if (f.PropertyType == typeof(bool))
            {
                if (c.GetType() == typeof(SqlConnection))
                    qvars += "BIT";
                else
                    qvars += "TINYINT(1)";
            }
            else if (f.PropertyType == typeof(DateOnly))
                qvars += "SMALLDATETIME";
            else if (f.PropertyType == typeof(DateTime))
                qvars += "DATETIME";
            else
            {
                qvars += "TEXT";
            }

            if (IsIdentity(f))
                keyList.Add(GetRealDataFieldName(f) + " ASC");

            if (IsIncrement(f))
                qvars += " IDENTITY";

            // Apply default
            var defa = GetDefaultValue(f);
            if (defa != null)
            {
                qvars += " NOT NULL DEFAULT " + defa; // make it not null 
            }


        }

        string keys = string.Empty;

        if (keyList.Count > 0)
        {
            keys = ", PRIMARY KEY CLUSTERED (" + string.Join(", ", keyList) + ") ";
        }

        DbCommand cmd;
        if (c.GetType() == typeof(SqlConnection))
        {
            cmd = new SqlCommand("CREATE TABLE " + tableName + " (" + qvars + keys + ")", (SqlConnection)c);
        }
        else
        {
            throw new Exception("No DB type supported.");
        }
        try
        {
            cmd.ExecuteNonQuery();
            cmd.Dispose();
            return true;
        }
        catch
        {
            return false;
        }
    }

    public string GetDefaultTable()
    {
        var att = this.GetType().GetCustomAttribute(typeof(MSQLTable));
        if (att != null)
        {
            MSQLTable table = (MSQLTable)att;
            return table.Name;
        }
        throw new Exception("Default table not specified.");
    }

    public List<string> QueryById(string idName = "ID")
    {
        List<string> q = new List<string>();
        q.Add(idName);
        return q;
    }

    public List<string> QueryByNN()
    {
        List<string> q = new List<string>();
        PropertyInfo[] fields = GetCachedProperties();
        foreach (PropertyInfo f in fields)
        {
            if (f.Name == "ID" || !IsDataField(f) || f.GetValue(this) == null || (f.PropertyType == typeof(int) && (int)f.GetValue(this) == 0) || f.PropertyType == typeof(DateTime))
                continue;
            q.Add(f.Name);
        }
        return q;
    }

    public string ToJsonString()
    {
        return JsonConvert.SerializeObject(this);
    }

    public string ToUTCJsonString()
    {
        return JsonConvert.SerializeObject(this,
                new JsonSerializerSettings
                {
                    DateTimeZoneHandling = DateTimeZoneHandling.Utc
                });
    }

    public string ToSyncJSONString()
    {
        return JsonConvert.SerializeObject(this,
                new JsonSerializerSettings
                {
                    DateTimeZoneHandling = DateTimeZoneHandling.Utc,
                    DateFormatString = "yyyy-MM-ddTHH:mm:ss.fffZ"

                });
    }

    private bool IsDataField(PropertyInfo f)
    {
        Attribute at = f.GetCustomAttribute(typeof(ModelLocal));
        return (at == null);
    }

    private string GetVarTypeInfo(PropertyInfo f)
    {
        VariableInfo at = (VariableInfo)f.GetCustomAttribute(typeof(VariableInfo));
        return (at != null ? at.TypeInfo : "");
    }

    private string? GetDefaultValue(PropertyInfo f)
    {
        var at = f.GetCustomAttribute<VariableInfo>();
        return at != null ? at.DefaultValue : null;
    }

    private bool IsModelLocal(PropertyInfo f)
    {
        Attribute at = f.GetCustomAttribute(typeof(ModelLocal));
        return (at != null);
    }

    private bool IsIncrement(PropertyInfo f)
    {
        Attribute at = f.GetCustomAttribute(typeof(Increment));
        return (at != null);
    }

    public string GetRealDataFieldName(PropertyInfo f)
    {
        Attribute at = f.GetCustomAttribute(typeof(ModelSerialize));
        if (at != null)
            return f.Name;
        if (f.PropertyType.IsSubclassOf(typeof(MSQLBaseModel)))
            return f.Name + "ID";
        else
            return f.Name;
    }

    private bool IsIdentity(PropertyInfo f)
    {
        Attribute at = f.GetCustomAttribute(typeof(Identity));
        return (at != null);
    }

    public string GetEditUrl()
    {
        ManagerModel at = (ManagerModel)this.GetType().GetCustomAttribute<ManagerModel>();
        if (at != null)
            return at.EditUrl;
        else
            return null;
    }

    public string ToSolidity()
    {
        string sol = "pragma solidity ^0.8.10;\n\n";
        sol += "contract " + this.GetType().Name + " {\n";
        PropertyInfo[] fields = GetCachedProperties();
        foreach (PropertyInfo f in fields)
        {
            if (IsModelLocal(f))
                continue;
            sol += "string public " + f.Name + " = \"" + f.GetValue(this) + "\";\n";

        }
        sol += "}\n";
        return sol;
    }

    public void StripJArrayFromIDs(JArray a)
    {
        foreach (var i in a)
        {
            if (i.Type == JTokenType.Object)
                StripJObjectFromIDs((JObject)i);
            else if (i.Type == JTokenType.Array)
                StripJArrayFromIDs((JArray)i);
        }
    }

    public void StripJObjectFromIDs(JObject o)
    {
        var props = new List<JProperty>();
        foreach (var i in o.Properties())
        {
            if (i.Type != JTokenType.Integer && i.Name.EndsWith("ID"))
                props.Add(i);
        }

        foreach (var prop in props)
        {
            prop.Remove();
        }

        foreach (var i in o.Children())
        {
            if (i.Type == JTokenType.Object)
                StripJObjectFromIDs((JObject)i);
            else if (i.Type == JTokenType.Array)
                StripJArrayFromIDs((JArray)i);
        }
    }

    public virtual string GetDataHash()
    {
        using (SHA256 sha256 = SHA256.Create())
        {
            string json = JsonConvert.SerializeObject(this);
            JObject o = (JObject)JsonConvert.DeserializeObject(json);
            StripJObjectFromIDs(o);
            byte[] data = sha256.ComputeHash(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(o)));
            return BitConverter.ToString(data);
        }
    }

    public static async Task<T?> GetFromIDAsync<T>(SQLBaseConnection db, int id) where T : MSQLBaseModel
    {
        var model = Activator.CreateInstance<T>();
        if (!model.SetProp("ID", id))
            throw new InvalidOperationException("Model does not have a writable ID property.");
        if (await model.SelectAsync(db.Connection, model.QueryById()))
            return model;
        return null;
    }

    public dynamic ToCamelCase()
    {
        dynamic o = new Dictionary<string, object>();
        var props = GetCachedProperties();
        foreach (var prop in props)
        {
            string camelName;
            if (prop.Name == "ID")
                camelName = "id";
            else if (prop.Name.Length > 1)
                camelName = char.ToLowerInvariant(prop.Name[0]) + prop.Name.Substring(1);
            else
                camelName = prop.Name.ToLower();
            o[camelName] = prop.GetValue(this);
        }
        return o;
    }

    public string GetPropDBType(PropertyInfo f)
    {
        if (f.Name == "ID")
        {
            return "ID INT PRIMARY KEY IDENTITY(1,1)";
        }

        var vtype = GetVarTypeInfo(f);

        if (!string.IsNullOrEmpty(vtype))
            return vtype;
        else if (f.PropertyType == typeof(int))
            return "INT";
        else if (f.PropertyType == typeof(long))
            return "BIGINT";
        else if (f.PropertyType == typeof(string))
        {
            if (GetVarTypeInfo(f).ToUpper() == "TEXT")
                return "TEXT";
            else
                return "VARCHAR(256)";
        }
        else if (f.PropertyType == typeof(float) || f.PropertyType == typeof(double))
            return "FLOAT";
        else if (f.PropertyType == typeof(decimal))
            return "DECIMAL(10,2)";
        else if (f.PropertyType == typeof(bool))
        {
            return "BIT NOT NULL DEFAULT 0";
        }
        else if (f.PropertyType == typeof(DateTime))
            return "DATETIME";
        else
            return "TEXT";

    }

    public string GetStructureSig()
    {
        string data = "C:" + this.GetType().Name;
        data += ";";
        data += "T:" + GetDefaultTable() ?? "";
        data += ";P:";
        PropertyInfo[] fields = GetCachedProperties();
        foreach (PropertyInfo f in fields)
        {
            if (IsModelLocal(f))
                continue;
            data += f.Name;
            data += ",";
            data += f.PropertyType.ToString();
            data += ";";
        }
        return data;
    }

    public bool TableExists(SqlConnection c)
    {
        string tableName = GetDefaultTable();

        using (var cmd = new SqlCommand(@"SELECT * FROM information_schema.columns WHERE table_name=@TableName", c))
        {
            cmd.Parameters.AddWithValue("@TableName", tableName);
            using (var r = cmd.ExecuteReader())
            {
                if (r.Read())
                {
                    return true;
                }
            }
            return false;
        }
    }

    public virtual void SyncStructure(SQLBaseConnection db)
    {
        string tableName = GetDefaultTable();
        SqlCommand cmd;
        bool exists = false;
        using (cmd = new SqlCommand(@"SELECT * FROM information_schema.columns WHERE table_name=@TableName", db.C))
        {
            cmd.Parameters.AddWithValue("@TableName", tableName);
            using (var r = cmd.ExecuteReader())
            {
                if (r.Read())
                {
                    exists = true;
                }
            }
        }

        if (!exists)
        {
            CreateTable(db.C);
        }
        else
        {
            PropertyInfo[] fields = GetCachedProperties();
            foreach (PropertyInfo f in fields)
            {
                if (IsModelLocal(f) || f.GetGetMethod() == null || !f.GetGetMethod().IsPublic)
                    continue;
                var colName = GetRealDataFieldName(f);
                exists = false;

                using (cmd = new SqlCommand(@"SELECT * FROM information_schema.columns WHERE table_name=@TableName AND column_name=@ColumnName", db.C))
                {
                    cmd.Parameters.AddWithValue("@TableName", tableName);
                    cmd.Parameters.AddWithValue("@ColumnName", colName);
                    exists = false;
                    using (var r = cmd.ExecuteReader())
                    {
                        if (r.Read())
                        {
                            exists = true;
                        }
                    }
                }

                if (exists)
                    continue;

                using (cmd = new SqlCommand(@"ALTER TABLE [" + tableName + "] ADD [" + colName + "] " + GetPropDBType(f), db.C))
                {
                    cmd.ExecuteNonQuery();
                }


            }
        }
        CreateIndexes(db);
        CreateFKeys(db.C);

    }

    /// <summary>
    /// Create index.
    /// </summary>
    /// <param name="db">The BaseConnection object instance.</param>
    /// <param name="indexDefinition">SQL index definition.</param>
    /// <param name="unique">Unique index?</param>
    /// <param name="indexName">SQL index name, if null, it will be auto-generated.</param>
    public void CreateIndex(SqlConnection c, string indexDefinition, bool unique = false, string indexName = null)
    {
        if (indexName == null)
        {
            var rgx = new Regex("[^a-zA-Z-]");
            indexName = rgx.Replace(GetDefaultTable() + "_" + indexDefinition, "");
        }
        ExecuteCommand(c, "CREATE " + (unique ? " UNIQUE " : "") + " INDEX " + indexName + " ON " + GetDefaultTable() + " (" + indexDefinition + ")");
    }



    // Compatibility
    public bool TryCreateIndex(SQLBaseConnection db, string indexDefinition, bool unique = false, string indexName = null)
    {
        try
        {
            CreateIndex(db.C, indexDefinition, unique, indexName);
            return true;
        }
        catch
        {
            return false;
        }
    }


    // Compatibility
    public virtual void CreateIndexes(SQLBaseConnection c)
    {

    }

    /// <summary>
    /// You can use [[DT]] to represent the default table.
    /// </summary>
    /// <param name="db">The BaseConnection object instance.</param>
    /// <param name="query">The SQL query.</param>
    public void ExecuteCommand(SqlConnection c, string query)
    {
        using (var cmd = new SqlCommand(query.Replace("[[DT]]", GetDefaultTable()), c))
        {
            cmd.ExecuteNonQuery();
        }
    }

    /// <summary>
    /// You can use [[DT]] to represent the default table.
    /// </summary>
    /// <param name="db">The BaseConnection object instance.</param>
    /// <param name="query">The SQL query.</param>
    /// <returns>true if sucess false if error</returns>
    public bool TryExecuteCommand(SqlConnection c, string query)
    {
        try
        {
            ExecuteCommand(c, query);
            return true;
        }
        catch
        {
            return false;
        }
    }

}