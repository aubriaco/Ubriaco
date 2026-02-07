/*---------------------------------------------------------------------------------------------
 *  Copyright (c) by Alessandro Ubriaco. All rights reserved.
 *--------------------------------------------------------------------------------------------*/



using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Npgsql;
using System.Data.Common;
using System.Diagnostics;
using System.Reflection;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Ubriaco.Attributes;
using Ubriaco.Data;

namespace Ubriaco.Models
{
    public class BaseModel
    {
        public static int SQLV = 6;

        public long ID { get; set; }

        [ModelLocal]
        public bool IsLegacy = false;

        public static DateTime MinDT = DateTime.Parse("1753-1-1");

        [ModelLocal]
        public string? Table;

        public BaseModel() { }

        public BaseModel(DbDataReader r)
        {
            Populate(r);
        }

        public virtual void PreInsert() { }
        public virtual void PostInsert() { }
        public virtual void PreUpdate() { }
        public virtual void PostUpdate() { }

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

        protected uint QGetUint(DbDataReader r, string n)
        {
            try
            {
                if (r[n] != null)
                    return Convert.ToUInt32(r[n]);
            }
            catch (Exception e)
            {
            }
            return 0;
        }


        public virtual void CopyFrom(BaseModel other)
        {
            PropertyInfo[] fields = this.GetType().GetProperties();
            foreach (PropertyInfo f in fields)
            {
                try
                {
                    f.SetValue(this, f.GetValue(other));
                }
                catch { }
            }
        }

        public virtual void EmptyNonEditable()
        {
            var defaultItem = Activator.CreateInstance(this.GetType());
            var fields = this.GetType().GetProperties();
            foreach (PropertyInfo f in fields)
            {
                if (!IsDataField(f) || !f.CanWrite || f.Name == "ID")
                    return;
                if (f.GetCustomAttribute<ColEditable>() == null)
                {
                    f.SetValue(this, f.GetValue(defaultItem));
                }
            }
        }

        public virtual void Populate(DbDataReader r)
        {
            PropertyInfo[] fields = this.GetType().GetProperties();
            foreach (PropertyInfo f in fields)
            {
                string vname = GetRealDataFieldName(f);

                try
                {
                    if (r[vname] == DBNull.Value)
                        continue;

                    if (f.PropertyType == typeof(int))
                        f.SetValue(this, QGetInt(r, vname));
                    else if (f.PropertyType == typeof(long))
                        f.SetValue(this, QGetLong(r, vname));
                    else if (f.PropertyType == typeof(uint))
                        f.SetValue(this, QGetUint(r, vname));
                    else if (f.PropertyType == typeof(double))
                        f.SetValue(this, Convert.ToDouble(r[vname]));
                    else if (f.PropertyType == typeof(decimal))
                        f.SetValue(this, Convert.ToDecimal(r[vname]));
                    else if (f.PropertyType == typeof(string))
                        f.SetValue(this, QGetString(r, vname));
                    else if (f.PropertyType == typeof(DateTime) && r[vname] != DBNull.Value)
                        f.SetValue(this, DateTime.Parse(QGetString(r, vname)));
                    else if (f.PropertyType == typeof(bool))
                        f.SetValue(this, r[vname] == DBNull.Value ? false : (QGetInt(r, vname) == 0 ? false : true));
                    else
                    {
                        var t = f.PropertyType;
                        f.SetValue(this, r[vname] == DBNull.Value ? null : JsonConvert.DeserializeObject(r[vname].ToString() ?? string.Empty, t));
                    }
                }
                catch (Exception e)
                {
                    if (vname == "ID")
                        Debug.WriteLine("Table has no ID.");
                    else
                        Debug.WriteLine(e.Message);
                }
            }

        }

        public virtual Dictionary<string, object> PopulateDict(DbDataReader r)
        {
            var dict = new Dictionary<string, object>();
            PropertyInfo[] fields = this.GetType().GetProperties();
            foreach (PropertyInfo f in fields)
            {
                string vname = GetRealDataFieldName(f);

                try
                {
                    if (r[vname] == DBNull.Value)
                        continue;

                    dict.Add(vname, r[vname]);

                }
                catch (Exception e)
                {
                    if (vname == "ID")
                        Debug.WriteLine("Table has no ID.");
                    else
                        Debug.WriteLine(e.Message);
                }
            }
            return dict;
        }

        public virtual void PopulateFromJsonElement(JsonElement o)
        {
            var inObj = (BaseModel?)o.Deserialize(this.GetType(), new JsonSerializerOptions()
            {
                PropertyNameCaseInsensitive = true
            });

            if (inObj == null)
                throw new Exception("PopulateFromJsonObject: Input object is null.");

            PropertyInfo[] fields = this.GetType().GetProperties();
            foreach (PropertyInfo f in fields)
            {
                if (f.Name == "MasterID")
                    continue;

                try
                {
                    f.SetValue(this, f.GetValue(inObj));
                }
                catch (Exception e)
                {
                    Debug.WriteLine(e.StackTrace);
                }
            }
        }

        public virtual void PopulateFromDynamic(dynamic o)
        {
            PropertyInfo[] fields = this.GetType().GetProperties();
            foreach (PropertyInfo f in fields)
            {
                //if (f.Name[0] == '_')
                //   continue;
                if (f.Name == "MasterID")
                    continue;

                string vname = GetRealDataFieldName(f);

                try
                {
                    dynamic i = o[vname];

                    if (f.PropertyType == typeof(int))
                        f.SetValue(this, Convert.ToInt32(i));
                    else if (f.PropertyType == typeof(double))
                        f.SetValue(this, Convert.ToDouble(i));
                    else if (f.PropertyType == typeof(decimal))
                        f.SetValue(this, Convert.ToDecimal(i));
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
            PropertyInfo[] fields = this.GetType().GetProperties();
            foreach (PropertyInfo f in fields)
            {
                if (f.Name == propName)
                {
                    if (f.PropertyType == typeof(int))
                        f.SetValue(this, Convert.ToInt32(value));
                    else if (f.PropertyType == typeof(long) || f.PropertyType == typeof(uint))
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
            PropertyInfo[] fields = this.GetType().GetProperties();
            foreach (PropertyInfo f in fields)
            {
                //if (f.Name[0] == '_')
                //   continue;
                if (f.Name == "MasterID")
                    continue;

                if (onlyEditable)
                {
                    Attribute? at = f.GetCustomAttribute(typeof(ColVisible));
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
                    else if (f.PropertyType == typeof(decimal))
                        f.SetValue(this, Convert.ToDecimal(o[f.Name]));
                }
                catch (Exception e)
                {
                    Debug.WriteLine(e.StackTrace);
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
                Debug.WriteLine(e.StackTrace);
                return;
            }
            PropertyInfo[] fields = this.GetType().GetProperties();
            foreach (PropertyInfo f in fields)
            {
                if (onlyEditable)
                {
                    Attribute? at = f.GetCustomAttribute(typeof(ColVisible));
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
                    else if (f.PropertyType == typeof(DateTime) && dict[f.Name] != null && Convert.ToString(dict[f.Name])?.Length > 0)
                        f.SetValue(this, DateTime.Parse(Convert.ToString(dict[f.Name]) ?? string.Empty));
                    else if (f.PropertyType == typeof(bool) && dict[f.Name] != null)
                        f.SetValue(this, Convert.ToBoolean(dict[f.Name]));
                    else if ((f.PropertyType == typeof(double) || f.PropertyType == typeof(float)) && dict[f.Name] != null)
                        f.SetValue(this, Convert.ToDouble(dict[f.Name]));
                    else if (f.PropertyType == typeof(decimal))
                        f.SetValue(this, Convert.ToDecimal(dict[f.Name]));
                }
                catch (Exception e)
                {
                    Debug.WriteLine("Error: " + e.Message);
                    Debug.WriteLine(e.StackTrace);
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
                Debug.WriteLine("Error: " + e.Message);
                Debug.WriteLine(e.StackTrace);
                return;
            }
            PropertyInfo[] fields = this.GetType().GetProperties();
            foreach (PropertyInfo f in fields)
            {
                if (f.Name == "MasterID")
                    continue;
                if (onlyEditable)
                {
                    Attribute? at = f.GetCustomAttribute(typeof(ColVisible));
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
                    else if (f.PropertyType == typeof(DateTime) && dict[f.Name] != null && Convert.ToString(dict[f.Name])?.Length > 0)
                        f.SetValue(this, DateTime.Parse(Convert.ToString(dict[f.Name]) ?? string.Empty));
                    else if (f.PropertyType == typeof(bool) && dict[f.Name] != null)
                        f.SetValue(this, Convert.ToBoolean(dict[f.Name]));
                    else if ((f.PropertyType == typeof(double) || f.PropertyType == typeof(float)) && dict[f.Name] != null)
                        f.SetValue(this, Convert.ToDouble(dict[f.Name]));
                    else if (f.PropertyType == typeof(decimal))
                        f.SetValue(this, Convert.ToDecimal(dict[f.Name]));
                }
                catch (Exception e)
                {
                    Debug.WriteLine("Error: " + e.Message);
                    Debug.WriteLine(e.StackTrace);
                }
            }
        }

        public virtual void PopulateFromModel(BaseModel other, bool onlyEditable = true)
        {
            PropertyInfo[] fields = this.GetType().GetProperties();
            foreach (PropertyInfo f in fields)
            {
                if (onlyEditable)
                {
                    Attribute? at = f.GetCustomAttribute(typeof(ColVisible));
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
                    f.SetValue(this, other.GetType()?.GetProperty(f.Name)?.GetValue(other));
                }
                catch (Exception e)
                {
                    Debug.WriteLine("Error: " + e.Message);
                    Debug.WriteLine(e.StackTrace);
                }
            }
        }


        public virtual void PutParams(NpgsqlCommand cmd, bool onlyEditable = false)
        {
            PropertyInfo[] fields = this.GetType().GetProperties();
            foreach (PropertyInfo f in fields)
            {
                if (!IsDataField(f))
                    continue;

                if (onlyEditable && f.GetCustomAttribute<ColEditable>() == null)
                    continue;

                if (f.GetValue(this) == null)
                    cmd.Parameters.AddWithValue(f.Name, DBNull.Value);
                else if (f.PropertyType == typeof(DateTime))
                {
                    var _dt = f.GetValue(this);
                    if (_dt == null)
                        cmd.Parameters.AddWithValue(f.Name, DBNull.Value);
                    else
                    {
                        var dt = (DateTime)_dt;
                        if (dt <= MinDT)
                            cmd.Parameters.AddWithValue(f.Name, DBNull.Value);
                        else
                            cmd.Parameters.AddWithValue(f.Name, dt);
                    }
                }
                else if (f.PropertyType == typeof(DateOnly))
                {
                    var _dt = f.GetValue(this);
                    if (_dt == null)
                        cmd.Parameters.AddWithValue(f.Name, DBNull.Value);
                    else
                    {
                        var dt = (DateOnly)_dt;
                        if (dt <= DateOnly.MinValue)
                            cmd.Parameters.AddWithValue(f.Name, DBNull.Value);
                        else
                            cmd.Parameters.AddWithValue(f.Name, dt);
                    }
                }
                else if (f.PropertyType == typeof(string))
                {
                    var _val = f.GetValue(this);
                    if (_val == null)
                        cmd.Parameters.AddWithValue(f.Name, DBNull.Value);
                    else
                        cmd.Parameters.AddWithValue(f.Name, (string)_val);
                }
                else if (f.PropertyType == typeof(int) || f.PropertyType == typeof(long) || f.PropertyType == typeof(double) || f.PropertyType == typeof(decimal))
                    cmd.Parameters.AddWithValue(f.Name, f.GetValue(this) ?? 0);
                else if (f.PropertyType == typeof(uint))
                    cmd.Parameters.AddWithValue(f.Name, Convert.ToInt64(f.GetValue(this) ?? 0));
                else if (f.PropertyType == typeof(bool))
                    cmd.Parameters.AddWithValue(f.Name, (bool)(f.GetValue(this) ?? false) ? true : false);
                else
                    cmd.Parameters.AddWithValue(f.Name, JsonConvert.SerializeObject(f.GetValue(this)));
            }
        }

        public virtual long Insert(BaseConnection c, string? tableName = null, bool identityInsert = false, bool onlyEditable = false)
        {
            return Insert(c.C, tableName, identityInsert, onlyEditable);
        }

        // COMMON ENTRY
        private long Insert(NpgsqlConnection c, string? tableName = null, bool identityInsert = false, bool onlyEditable = false)
        {
            if (tableName == null) tableName = GetDefaultTable();
            string qvars = "", vvars = "";
            PropertyInfo[] fields = this.GetType().GetProperties();
            foreach (PropertyInfo f in fields)
            {
                if ((f.Name == "ID" && !identityInsert) || !IsDataField(f))
                    continue;
                if (onlyEditable && f.GetCustomAttribute<ColEditable>() == null)
                    continue;
                if (qvars.Length > 0)
                {
                    qvars += ",";
                    vvars += ",";
                }
                qvars += GetRealDataFieldName(f);
                vvars += "@" + f.Name;
            }
            using (NpgsqlCommand cmd = new NpgsqlCommand("INSERT INTO " + GetDefaultSchema() + "." + tableName + " (" + qvars + ") VALUES (" + vvars + ") RETURNING ID", c))
            {
                PutParams(cmd, onlyEditable);
                var r = cmd.ExecuteScalar();
                return r == DBNull.Value ? 0 : Convert.ToInt64(r);
            }
        }

        // COMMON ENTRY ASYNC
        public virtual async Task<long> InsertAsync(BaseConnection c, string? tableName = null, bool identityInsert = false, bool onlyEditable = false, UTransaction? tx = null)
        {
            if (tableName == null) tableName = GetDefaultTable();
            string qvars = "", vvars = "";
            PropertyInfo[] fields = this.GetType().GetProperties();
            foreach (PropertyInfo f in fields)
            {
                if ((f.Name == "ID" && !identityInsert) || !IsDataField(f))
                    continue;
                if (onlyEditable && f.GetCustomAttribute<ColEditable>() == null)
                    continue;
                if (qvars.Length > 0)
                {
                    qvars += ",";
                    vvars += ",";
                }
                qvars += GetRealDataFieldName(f);
                vvars += "@" + f.Name;
            }
            using (NpgsqlCommand cmd = new NpgsqlCommand("INSERT INTO " + GetDefaultSchema() + "." + tableName + " (" + qvars + ") VALUES (" + vvars + ") RETURNING ID", c.C, tx != null ? tx.T : null))
            {
                PutParams(cmd, onlyEditable);
                var r = await cmd.ExecuteScalarAsync();
                return r == DBNull.Value ? 0 : Convert.ToInt64(r);
            }
        }

        public virtual Task<long> InsertTXAsync(BaseConnection c, UTransaction tx, bool identityInsert = false, bool onlyEditable = false)
        {
            return InsertAsync(c, null, false, false, tx);
        }

        public virtual async Task<long> TryInsertAsync(BaseConnection c, string? tableName = null, bool identityInsert = false, bool onlyEditable = false)
        {
            try
            {
                return await InsertAsync(c, tableName, identityInsert, onlyEditable);
            }
            catch (Exception e)
            {
                Debug.WriteLine("Error: " + e.Message);
                Debug.WriteLine(e.StackTrace);
                return 0;
            }
        }

        private long Insert(DbConnection c, string? tableName = null, bool identityInsert = false, bool onlyEditable = false)
        {
            if (c.GetType() == typeof(NpgsqlConnection))
                return Insert((NpgsqlConnection)c, tableName, identityInsert, onlyEditable);
            throw new Exception("No DB type supported.");
        }


        public virtual bool InsertNonQuery(NpgsqlConnection c, string? tableName = null)
        {
            if (tableName == null) tableName = GetDefaultTable();
            string qvars = "", vvars = "";
            PropertyInfo[] fields = this.GetType().GetProperties();
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
                using (NpgsqlCommand cmd = new NpgsqlCommand("INSERT INTO " + GetDefaultSchema() + "." + tableName + " (" + qvars + ") VALUES (" + vvars + ") RETURNING ID", c))
                {
                    PutParams(cmd);
                    cmd.ExecuteNonQuery();
                    return true;
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine("Error: " + e.Message);
                Debug.WriteLine(e.StackTrace);
                return false;
            }
        }



        public virtual bool DeleteFromId(NpgsqlConnection c, string? tableName = null)
        {
            if (tableName == null) tableName = GetDefaultTable();
            try
            {
                using (NpgsqlCommand cmd = new NpgsqlCommand("DELETE FROM " + GetDefaultSchema() + "." + tableName + " WHERE id=@ID", c))
                {
                    cmd.Parameters.AddWithValue("@ID", ID);
                    cmd.ExecuteNonQuery();
                    return true;
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine("Error: " + e.Message);
                Debug.WriteLine(e.StackTrace);
                return false;
            }
        }

        public virtual async Task<bool> DeleteFromIdAsync(NpgsqlConnection c, string? tableName = null, UTransaction? tx = null)
        {
            if (tableName == null) tableName = GetDefaultTable();
            try
            {
                using (NpgsqlCommand cmd = new NpgsqlCommand("DELETE FROM " + GetDefaultSchema() + "." + tableName + " WHERE id=@ID", c, tx != null ? tx.T : null))
                {
                    cmd.Parameters.AddWithValue("@ID", ID);
                    await cmd.ExecuteNonQueryAsync();
                    return true;
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine("Error: " + e.Message);
                Debug.WriteLine(e.StackTrace);
                return false;
            }
        }

        public virtual Task<bool> DeleteFromIdTXAsync(BaseConnection c, UTransaction tx)
        {
            return DeleteFromIdAsync((NpgsqlConnection)c.C, null, tx);
        }

        public virtual Task<bool> DeleteFromIdAsync(BaseConnection c, string? tableName = null)
        {
            return DeleteFromIdAsync((NpgsqlConnection)c.C, tableName);
        }

        public virtual bool DeleteFromId(BaseConnection c, string? tableName = null)
        {
            return DeleteFromId((NpgsqlConnection)c.C, tableName);
            throw new Exception("No DB type supported.");
        }

        public virtual bool DeleteFromId(DbConnection c, string? tableName = null)
        {
            if (c.GetType() == typeof(NpgsqlConnection))
                return DeleteFromId((NpgsqlConnection)c, tableName);
            throw new Exception("No DB type supported.");
        }

        public virtual bool DeleteFromNN(NpgsqlConnection c, string? tableName = null)
        {
            if (tableName == null) tableName = GetDefaultTable();
            string qvars = "";
            PropertyInfo[] fields = this.GetType().GetProperties();
            foreach (PropertyInfo f in fields)
            {
                if (f.Name == "ID" || !IsDataField(f))
                    continue;
                if (qvars.Length > 0)
                    qvars += " AND ";
                qvars += GetRealDataFieldName(f) + "=@" + f.Name;
            }
            try
            {
                using (NpgsqlCommand cmd = new NpgsqlCommand("DELETE FROM " + GetDefaultSchema() + "." + tableName + " WHERE " + qvars, c))
                {
                    PutParams(cmd);
                    cmd.ExecuteNonQuery();
                    return true;
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine("Error: " + e.Message);
                Debug.WriteLine(e.StackTrace);
                return false;
            }
        }

        public virtual void Update(BaseConnection c)
        {
            Update(c.C);
        }


        private void Update(DbConnection c, bool onlyEditable = false)
        {
            string tableName = GetDefaultTable();
            string idName = "id";
            string qvars = "";
            PropertyInfo[] fields = this.GetType().GetProperties();
            foreach (PropertyInfo f in fields)
            {
                if (f.Name == "ID" || !IsDataField(f) || IsIdentity(f))
                    continue;
                if (onlyEditable && f.GetCustomAttribute<ColEditable>() == null)
                    continue;
                if (qvars.Length > 0)
                {
                    qvars += ",";
                }
                qvars += GetRealDataFieldName(f) + "=@" + f.Name;
            }
            DbCommand cmd;
            if (c.GetType() == typeof(NpgsqlConnection))
            {
                using (cmd = new NpgsqlCommand("UPDATE " + GetDefaultSchema() + "." + tableName + " SET " + qvars + " WHERE " + idName + "=" + ID, (NpgsqlConnection)c))
                {
                    PutParams((NpgsqlCommand)cmd, onlyEditable);
                    cmd.ExecuteNonQuery();
                }
            }
            else
            {
                throw new Exception("No DB type supported.");
            }
        }

        // COMMON ENTRY

        public async Task UpdateAsync(BaseConnection c, bool onlyEditable = false, UTransaction? tx = null)
        {
            if (c.C == null)
                throw new Exception("Connection is null.");
            string tableName = GetDefaultTable();
            string idName = "id";
            string qvars = "";
            PropertyInfo[] fields = this.GetType().GetProperties();
            foreach (PropertyInfo f in fields)
            {
                if (f.Name == "ID" || !IsDataField(f) || IsIdentity(f))
                    continue;
                if (onlyEditable && f.GetCustomAttribute<ColEditable>() == null)
                    continue;
                if (qvars.Length > 0)
                {
                    qvars += ",";
                }
                qvars += GetRealDataFieldName(f) + "=@" + f.Name;
            }
            DbCommand cmd;
            if (c.C.GetType() == typeof(NpgsqlConnection))
            {
                using (cmd = new NpgsqlCommand("UPDATE " + GetDefaultSchema() + "." + tableName + " SET " + qvars + " WHERE " + idName + "=" + ID, (NpgsqlConnection)c.C, tx != null ? tx.T : null))
                {
                    PutParams((NpgsqlCommand)cmd, onlyEditable);
                    await cmd.ExecuteNonQueryAsync();
                }
            }
            else
            {
                throw new Exception("No DB type supported.");
            }
        }

        public Task UpdateTXAsync(BaseConnection c, UTransaction? tx = null)
        {
            return UpdateAsync(c, false, tx);
        }

        // COMMON ENTRY
        public virtual void Update(BaseConnection c, string q, string? tableName = null, string idName = "ID")
        {
            string[] qq;
            if (q.Contains(','))
                qq = q.Split(',');
            else
                qq = new string[] { q };

            Update(c.C, new List<string>(qq), tableName, idName);
        }

        // COMMON ENTRY ASYNC
        public virtual async Task UpdateAsync(BaseConnection c, string q, string? tableName = null, string idName = "ID")
        {
            string[] qq;
            if (q.Contains(','))
                qq = q.Split(',');
            else
                qq = new string[] { q };

            await _UpdateAsync(c.C, new List<string>(qq), tableName, idName);
        }

        public virtual async Task _UpdateAsync(DbConnection c, List<string> q, string? tableName = null, string? idName = null, List<string>? exclude = null)
        {
            if (tableName == null) tableName = GetDefaultTable();
            string qvars = "";
            PropertyInfo[] fields = this.GetType().GetProperties();
            foreach (PropertyInfo f in fields)
            {
                if (f.Name == "ID" || !IsDataField(f) || IsIdentity(f) || !q.Contains(f.Name))
                    continue;
                if (exclude != null && exclude.Contains(f.Name))
                    continue;
                if (qvars.Length > 0)
                {
                    qvars += ",";
                }
                qvars += GetRealDataFieldName(f) + "=@" + f.Name;
            }
            DbCommand cmd;
            if (c.GetType() == typeof(NpgsqlConnection))
            {
                using (cmd = new NpgsqlCommand("UPDATE " + GetDefaultSchema() + "." + tableName + " SET " + qvars + " WHERE " + idName + "=" + ID, (NpgsqlConnection)c))
                {
                    PutParams((NpgsqlCommand)cmd);
                    await cmd.ExecuteNonQueryAsync();
                }
            }
            else
            {
                throw new Exception("No DB type supported.");
            }
        }


        public virtual void Update(BaseConnection c, List<string> q, string? tableName = null, string idName = "ID")
        {
            Update(c.C, q, tableName, idName);
        }

        private void Update(DbConnection c, List<string> q, string? tableName = null, string idName = "ID")
        {
            Update(c, q, tableName, idName, null);
        }

        private void Update(DbConnection c, List<string> q, string? tableName, string idName, List<string>? exclude)
        {
            if (tableName == null) tableName = GetDefaultTable();
            string qvars = "";
            PropertyInfo[] fields = this.GetType().GetProperties();
            foreach (PropertyInfo f in fields)
            {
                if (f.Name == "ID" || !IsDataField(f) || IsIdentity(f) || !q.Contains(f.Name))
                    continue;
                if (exclude != null && exclude.Contains(f.Name))
                    continue;
                if (qvars.Length > 0)
                {
                    qvars += ",";
                }
                qvars += GetRealDataFieldName(f) + "=@" + f.Name;
            }
            DbCommand cmd;
            if (c.GetType() == typeof(NpgsqlConnection))
            {
                using (cmd = new NpgsqlCommand("UPDATE " + GetDefaultSchema() + "." + tableName + " SET " + qvars + " WHERE " + idName + "=" + ID, (NpgsqlConnection)c))
                {
                    PutParams((NpgsqlCommand)cmd);
                    cmd.ExecuteNonQuery();
                }
            }
            else
            {
                throw new Exception("No DB type supported.");
            }
        }

        public virtual void UpdateByParams(BaseConnection c, List<string> q, List<string> r, string? tableName = null)
        {
            UpdateByParams(c.C, q, r, tableName);
        }

        private void UpdateByParams(DbConnection c, List<string> q, List<string> r, string? tableName = null)
        {
            if (tableName == null) tableName = GetDefaultTable();
            string qvars = "", rvars = "";
            PropertyInfo[] fields = this.GetType().GetProperties();
            foreach (PropertyInfo f in fields)
            {
                if (f.Name != "ID" && IsDataField(f) && !IsIdentity(f) && q.Contains(f.Name))
                {
                    if (qvars.Length > 0)
                        qvars += ",";
                    qvars += GetRealDataFieldName(f) + "=@" + f.Name;
                }
                if (r.Contains(f.Name))
                {
                    if (rvars.Length > 0)
                        rvars += " AND ";
                    rvars += GetRealDataFieldName(f) + "=@" + f.Name;
                }
            }
            if (rvars.Length == 0)
                throw new Exception("RVARS is empty.");
            DbCommand cmd;
            if (c.GetType() == typeof(NpgsqlConnection))
            {
                using (cmd = new NpgsqlCommand("UPDATE " + GetDefaultSchema() + "." + tableName + " SET " + qvars + " WHERE " + rvars, (NpgsqlConnection)c))
                {
                    PutParams((NpgsqlCommand)cmd);
                    cmd.ExecuteNonQuery();
                }
            }
            else
            {
                throw new Exception("No DB type supported.");
            }
        }

        public virtual bool Select(BaseConnection c, string[] q, string? tableName = null, string? orderBy = null)
        {
            return Select(c, new List<string>(q), tableName, orderBy);
        }

        public virtual bool Select(BaseConnection c, List<string> q, string? tableName = null, string? orderBy = null)
        {
            return Select(c.C, q, tableName, orderBy);
        }

        public virtual bool Select(BaseConnection c, string q, string? tableName = null, string? orderBy = null)
        {
            string[] qq;
            if (q.Contains(','))
                qq = q.Split(',');
            else
                qq = new string[] { q };
            return Select(c, qq, tableName, orderBy);
        }

        public virtual async Task<bool> SelectAsync(BaseConnection c, string _q, string? tableName = null, string? orderBy = null)
        {
            if (c.C == null)
                throw new Exception("Connection is null.");
            string[] q;
            if (_q.Contains(','))
                q = _q.Split(',');
            else
                q = new string[] { _q };

            if (tableName == null) tableName = GetDefaultTable();
            PropertyInfo[] fields = this.GetType().GetProperties();
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
            if (c.C.GetType() == typeof(NpgsqlConnection))
            {
                using (cmd = new NpgsqlCommand("SELECT * FROM " + GetDefaultSchema() + "." + tableName + " WHERE " + qvars + (orderBy != null ? " ORDER BY " + orderBy : "") + " LIMIT 1 ", (NpgsqlConnection)c.C))
                {
                    PutParams((NpgsqlCommand)cmd);
                    using (DbDataReader r = await cmd.ExecuteReaderAsync())
                    {
                        if (await r.ReadAsync())
                        {
                            Populate(r);
                            return true;
                        }
                    }
                }
            }
            else
            {
                throw new Exception("No DB type supported.");
            }
            return false;
        }


        private bool Select(DbConnection c, List<string> q, string? tableName = null, string? orderBy = null)
        {
            if (tableName == null) tableName = GetDefaultTable();
            PropertyInfo[] fields = this.GetType().GetProperties();
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
            if (c.GetType() == typeof(NpgsqlConnection))
            {
                using (cmd = new NpgsqlCommand("SELECT * FROM " + GetDefaultSchema() + "." + tableName + " WHERE " + qvars + (orderBy != null ? " ORDER BY " + orderBy : "") + " LIMIT 1 ", (NpgsqlConnection)c))
                {
                    PutParams((NpgsqlCommand)cmd);
                    using (DbDataReader r = cmd.ExecuteReader())
                    {
                        if (r.Read())
                        {
                            Populate(r);
                            return true;
                        }
                    }
                }
            }
            else
            {
                throw new Exception("No DB type supported.");
            }
            return false;
        }

        public virtual bool SelectID(BaseConnection c, string q, string? tableName = null, string? orderBy = null)
        {
            string[] qq;
            if (q.Contains(','))
                qq = q.Split(',');
            else
                qq = new string[] { q };
            return SelectID(c.C, qq, tableName, orderBy);
        }

        public virtual async Task<bool> SelectIDAsync(BaseConnection c, string _q, string? tableName = null, string? orderBy = null)
        {
            return await SelectIDAsync(c.C, _q, tableName, orderBy);
        }

        public virtual async Task<bool> SelectIDAsync(DbConnection c, string _q, string? tableName = null, string? orderBy = null)
        {
            string[] q;
            if (_q.Contains(','))
                q = _q.Split(',');
            else
                q = new string[] { _q };
            if (tableName == null) tableName = GetDefaultTable();
            PropertyInfo[] fields = this.GetType().GetProperties();
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
            if (c.GetType() == typeof(NpgsqlConnection))
            {
                using (cmd = new NpgsqlCommand("SELECT id FROM " + GetDefaultSchema() + "." + tableName + " WHERE " + qvars + (orderBy != null ? " ORDER BY " + orderBy : "") + " LIMIT 1 ", (NpgsqlConnection)c))
                {
                    PutParams((NpgsqlCommand)cmd);
                    using (DbDataReader r = await cmd.ExecuteReaderAsync())
                    {
                        if (await r.ReadAsync())
                        {
                            Populate(r);
                            return true;
                        }
                    }
                }
            }
            else
            {
                throw new Exception("No DB type supported.");
            }
            return false;
        }


        private bool SelectID(DbConnection c, string[] q, string? tableName = null, string? orderBy = null)
        {
            if (tableName == null) tableName = GetDefaultTable();
            PropertyInfo[] fields = this.GetType().GetProperties();
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
            if (c.GetType() == typeof(NpgsqlConnection))
            {
                using (cmd = new NpgsqlCommand("SELECT id FROM " + GetDefaultSchema() + "." + tableName + " WHERE " + qvars + (orderBy != null ? " ORDER BY " + orderBy : "") + " LIMIT 1 ", (NpgsqlConnection)c))
                {
                    PutParams((NpgsqlCommand)cmd);
                    using (DbDataReader r = cmd.ExecuteReader())
                    {
                        if (r.Read())
                        {
                            Populate(r);
                            return true;
                        }
                    }
                }
            }
            else
            {
                throw new Exception("No DB type supported.");
            }
            return false;
        }

        public virtual List<T> List<T>(BaseConnection c, string q, string? tableName = null, string? sort = null, int limit = 0) where T : BaseModel
        {
            string[] qq;
            if (q.Contains(','))
                qq = q.Split(',');
            else
                qq = new string[] { q };



            return List<T>(c, new List<string>(qq), tableName, sort, limit);
        }

        public virtual async Task<List<T>> ListAsync<T>(BaseConnection c, string q, string? tableName = null, string? sort = null, int limit = 0) where T : BaseModel
        {
            if (c.C == null)
                throw new Exception("Connection is null.");
            List<T> list = new List<T>();
            if (tableName == null) tableName = GetDefaultTable();
            PropertyInfo[] fields = this.GetType().GetProperties();
            string qvars = "";
            if (q != null)
            {
                string[] qq;
                if (q.Contains(','))
                    qq = q.Split(',');
                else
                    qq = new string[] { q };

                foreach (PropertyInfo f in fields)
                {
                    if (!qq.Contains(f.Name))
                        continue;
                    if (qvars.Length > 0)
                    {
                        qvars += " AND ";
                    }
                    qvars += GetRealDataFieldName(f) + "=@" + f.Name;
                }
            }
            DbCommand cmd;
            if (c.C.GetType() == typeof(NpgsqlConnection))
            {
                using (cmd = new NpgsqlCommand("SELECT * FROM " + GetDefaultSchema() + "." + tableName + (qvars.Length > 0 ? " WHERE " + qvars : "") + (sort != null ? " ORDER BY " + sort : "") + (limit != 0 ? " LIMIT " + limit : ""), (NpgsqlConnection)c.C))
                {
                    PutParams((NpgsqlCommand)cmd);
                    using (DbDataReader r = await cmd.ExecuteReaderAsync())
                    {
                        while (await r.ReadAsync())
                        {
                            T? item = (T?)Activator.CreateInstance(this.GetType());
                            if (item == null)
                                throw new Exception("Model is null.");
                            item.Populate(r);
                            list.Add(item);
                        }
                    }
                }
            }
            else
            {
                throw new Exception("No DB type supported.");
            }
            return list;
        }


        public virtual async Task<List<Dictionary<string, object>>> DataAsync<T>(BaseConnection c, string q, string? tableName = null, string? sort = null, int limit = 0) where T : BaseModel
        {
            if (c.C == null)
                throw new Exception("Connection is null.");
            List<Dictionary<string, object>> list = new List<Dictionary<string, object>>();
            if (tableName == null) tableName = GetDefaultTable();
            PropertyInfo[] fields = this.GetType().GetProperties();
            string qvars = "";
            if (q != null)
            {
                string[] qq;
                if (q.Contains(','))
                    qq = q.Split(',');
                else
                    qq = new string[] { q };

                foreach (PropertyInfo f in fields)
                {
                    if (!qq.Contains(f.Name))
                        continue;
                    if (qvars.Length > 0)
                    {
                        qvars += " AND ";
                    }
                    qvars += GetRealDataFieldName(f) + "=@" + f.Name;
                }
            }
            DbCommand cmd;
            if (c.C.GetType() == typeof(NpgsqlConnection))
            {
                using (cmd = new NpgsqlCommand("SELECT * FROM " + GetDefaultSchema() + "." + tableName + (qvars.Length > 0 ? " WHERE " + qvars : "") + (sort != null ? " ORDER BY " + sort : "") + (limit != 0 ? " LIMIT " + limit : ""), (NpgsqlConnection)c.C))
                {
                    PutParams((NpgsqlCommand)cmd);
                    using (DbDataReader r = await cmd.ExecuteReaderAsync())
                    {
                        while (await r.ReadAsync())
                        {
                            var dict = PopulateDict(r);
                            list.Add(dict);
                        }
                    }
                }
            }
            else
            {
                throw new Exception("No DB type supported.");
            }
            return list;
        }

        public virtual List<T> List<T>(BaseConnection c) where T : BaseModel
        {
            return List<T>(c.C, null, null, null, 0);
        }

        public virtual List<T> List<T>(BaseConnection c, List<string>? q, string? tableName = null, string? sort = null, int limit = 0) where T : BaseModel
        {
            if (q != null && q.Count != 0 && q[0] == "")
                q = null;
            return List<T>(c.C, q, tableName, sort, limit);
        }

        public virtual List<BaseModel> ListBase(BaseConnection c, List<string> q, string? tableName = null, string? sort = null, int limit = 0)
        {
            List<BaseModel> list = new List<BaseModel>();
            if (tableName == null) tableName = GetDefaultTable();
            PropertyInfo[] fields = this.GetType().GetProperties();
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
            if (c.C.GetType() == typeof(NpgsqlConnection))
            {
                using (cmd = new NpgsqlCommand("SELECT * FROM " + GetDefaultSchema() + "." + tableName + (qvars.Length > 0 ? " WHERE " + qvars : "") + (sort != null ? " ORDER BY " + sort : "") + (limit != 0 ? " LIMIT " + limit : ""), (NpgsqlConnection)c.C))
                {
                    PutParams((NpgsqlCommand)cmd);
                    using (DbDataReader r = cmd.ExecuteReader())
                    {
                        while (r.Read())
                        {
                            var item = (BaseModel?)Activator.CreateInstance(this.GetType());
                            if (item == null)
                                throw new Exception("Model is null.");
                            item.Populate(r);
                            list.Add(item);
                        }
                    }
                }
            }
            else
            {
                throw new Exception("No DB type supported.");
            }
            return list;
        }

        private List<T> List<T>(DbConnection c, List<string>? q, string? tableName = null, string? sort = null, int limit = 0) where T : BaseModel
        {
            List<T> list = new List<T>();
            if (tableName == null) tableName = GetDefaultTable();
            PropertyInfo[] fields = this.GetType().GetProperties();
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
            if (c.GetType() == typeof(NpgsqlConnection))
            {
                using (cmd = new NpgsqlCommand("SELECT * FROM " + GetDefaultSchema() + "." + tableName + (qvars.Length > 0 ? " WHERE " + qvars : "") + (sort != null ? " ORDER BY " + sort : "") + (limit != 0 ? " LIMIT " + limit : ""), (NpgsqlConnection)c))
                {
                    PutParams((NpgsqlCommand)cmd);
                    using (DbDataReader r = cmd.ExecuteReader())
                    {
                        while (r.Read())
                        {
                            T? item = (T?)Activator.CreateInstance(this.GetType());
                            if (item == null)
                                throw new Exception("Model is null.");
                            item.Populate(r);
                            list.Add(item);
                        }
                    }
                }
            }
            else
            {
                throw new Exception("No DB type supported.");
            }
            return list;
        }

        public string? GetPropDBType(PropertyInfo f)
        {
            if (f.Name == "ID")
            {
                return "ID SERIAL PRIMARY KEY";
            }

            var at = (VariableInfo?)f.GetCustomAttribute(typeof(VariableInfo));
            var vtype = at != null && at.TypeInfo != null ? at.TypeInfo : "";

            if (!string.IsNullOrEmpty(vtype))
                return vtype;
            else if (f.PropertyType == typeof(int))
                return "INT";
            else if (f.PropertyType == typeof(long) || f.PropertyType == typeof(uint))
                return "BIGINT";
            else if (f.PropertyType == typeof(string))
            {
                if (vtype.ToUpper() == "TEXT")
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
                return "BOOLEAN";
            }
            else if (f.PropertyType == typeof(DateTime))
            {
                if (at != null && at.NoTimeZone)
                    return "TIMESTAMP" + (SQLV >= 6 ? " WITHOUT TIME ZONE" : "");
                else
                    return "TIMESTAMP" + (SQLV >= 6 ? " WITH TIME ZONE" : "");
            }
            else if (f.PropertyType == typeof(DateOnly))
            {
                return "DATE";
            }
            else
                return "TEXT";

        }

        public virtual bool CreateTable(BaseConnection c, bool includeExternalID = false)
        {
            return CreateTable(c.C, false);
        }

        private bool CreateTable(DbConnection c, bool includeExternalID = false)
        {
            string tableName = GetDefaultTable();
            PropertyInfo[] fields = this.GetType().GetProperties();
            string qvars = "";
            foreach (PropertyInfo f in fields)
            {
                if (IsModelLocal(f))
                    continue;
                if (f.Name == "ID")
                {
                    if (c.GetType() == typeof(NpgsqlConnection))
                        qvars = "ID SERIAL PRIMARY KEY, " + qvars;
                    else
                        throw new Exception("No DB type supported.");
                    continue;
                }
                if (qvars.Length > 0)
                {
                    qvars += ", ";
                }
                if (c.GetType() == typeof(NpgsqlConnection))
                    qvars += GetRealDataFieldName(f) + " ";
                else
                    throw new Exception("No DB type supported.");

                qvars += GetPropDBType(f) + " " + GetForeignKey(f);

            }

            DbCommand cmd;
            if (c.GetType() == typeof(NpgsqlConnection))
            {
                using (cmd = new NpgsqlCommand("CREATE TABLE " + GetDefaultSchema() + "." + tableName + " (" + qvars + ")", (NpgsqlConnection)c))
                {
                    try
                    {
                        cmd.ExecuteNonQuery();
                        return true;
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("ERROR: " + e.Message);
                        return false;
                    }
                }
            }
            else
            {
                throw new Exception("No DB type supported.");
            }
        }

        public virtual Task<bool> CreateTableAsync(BaseConnection c, bool includeExternalID = false)
        {
            return CreateTableAsync(c.C, false);
        }

        private async Task<bool> CreateTableAsync(DbConnection c, bool includeExternalID = false)
        {
            string tableName = GetDefaultTable();
            PropertyInfo[] fields = this.GetType().GetProperties();
            string qvars = "";
            foreach (PropertyInfo f in fields)
            {
                if (IsModelLocal(f))
                    continue;
                if (f.Name == "ID")
                {
                    if (c.GetType() == typeof(NpgsqlConnection))
                        qvars = "ID SERIAL PRIMARY KEY, " + qvars;
                    else
                        throw new Exception("No DB type supported.");
                    continue;
                }
                if (qvars.Length > 0)
                {
                    qvars += ", ";
                }
                if (c.GetType() == typeof(NpgsqlConnection))
                    qvars += GetRealDataFieldName(f) + " ";
                else
                    throw new Exception("No DB type supported.");

                qvars += GetPropDBType(f) + " " + GetForeignKey(f);

            }

            DbCommand cmd;
            if (c.GetType() == typeof(NpgsqlConnection))
            {
                using (cmd = new NpgsqlCommand("CREATE TABLE " + GetDefaultSchema() + "." + tableName + " (" + qvars + ")", (NpgsqlConnection)c))
                {
                    try
                    {
                        await cmd.ExecuteNonQueryAsync();
                        return true;
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("ERROR: " + e.Message);
                        return false;
                    }
                }
            }
            else
            {
                throw new Exception("No DB type supported.");
            }
        }

        public string GetDefaultTable()
        {
            Attribute? att = this.GetType().GetCustomAttribute(typeof(Table));
            if (att != null)
            {
                Table table = (Table)att;
                return table.Name.ToLower();
            }
            throw new Exception("No table name specified.");
        }

        public string GetDefaultSchema()
        {
            Attribute? att = this.GetType().GetCustomAttribute(typeof(Schema));
            if (att != null)
            {
                Schema s = (Schema)att;
                return s.Name.ToLower();
            }
            return "public";
        }

        public string[] QueryById(string idName = "ID")
        {
            return new string[] { idName };
        }

        public List<string> QueryByNN()
        {
            List<string> q = new List<string>();
            PropertyInfo[] fields = this.GetType().GetProperties();
            foreach (PropertyInfo f in fields)
            {
                if (f.Name == "ID" || !IsDataField(f) || f.GetValue(this) == null || (f.PropertyType == typeof(int) && (int?)f.GetValue(this) == 0) || f.PropertyType == typeof(DateTime))
                    continue;
                q.Add(GetRealDataFieldName(f));
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
            Attribute? at = f.GetCustomAttribute(typeof(ModelLocal));
            return (at == null);
        }

        //removing
        private string? GetVarTypeInfo(PropertyInfo f)
        {
            VariableInfo? at = (VariableInfo?)f.GetCustomAttribute(typeof(VariableInfo));
            return (at != null && at.TypeInfo != null ? at.TypeInfo : "");
        }

        private string? GetForeignKey(PropertyInfo f)
        {
            var at = f.GetCustomAttribute<VariableInfo>();
            return (at != null && at.ForeignKey != null ? " REFERENCES " + at.ForeignKey : "");
        }

        private bool IsModelLocal(PropertyInfo f)
        {
            Attribute? at = f.GetCustomAttribute(typeof(ModelLocal));
            return (at != null);
        }

        protected string GetRealDataFieldName(PropertyInfo f)
        {
            Attribute? at = f.GetCustomAttribute(typeof(LegacyVariable));
            if (at != null && IsLegacy)
            {
                LegacyVariable dbv = (LegacyVariable)at;
                if (dbv.RealVar != null)
                    return dbv.RealVar;
            }
            //if (f.PropertyType.IsSubclassOf(typeof(BaseModel)))
            //    return f.Name.ToLower() + "_id";
            //else
            return f.Name.ToLower();
        }

        private bool IsIdentity(PropertyInfo f)
        {
            Attribute? at = f.GetCustomAttribute(typeof(Identity));
            return (at != null);
        }


        public string? GetEditUrl()
        {
            var at = (ManagerModel?)this.GetType().GetCustomAttribute<ManagerModel>();
            if (at != null)
                return at.EditUrl;
            else
                return null;
        }

        public string GetStructureSig()
        {
            string data = "C:" + this.GetType().Name;
            data += ";";
            data += "T:" + GetDefaultTable() ?? "";
            data += ";P:";
            PropertyInfo[] fields = this.GetType().GetProperties();
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

        public bool TableExists(BaseConnection c)
        {
            var tableName = GetDefaultTable();
            using (var cmd = new NpgsqlCommand(@"SELECT * FROM information_schema.columns WHERE table_schema=@SchemaName AND table_name=@TableName", c.C))
            {
                cmd.Parameters.AddWithValue("@SchemaName", GetDefaultSchema());
                cmd.Parameters.AddWithValue("@TableName", tableName);
                using (var r = cmd.ExecuteReader())
                {
                    if (r.Read())
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        public virtual void SyncStructure(BaseConnection c)
        {
            string tableName = GetDefaultTable();
            NpgsqlCommand cmd;
            bool exists = false;
            using (cmd = new NpgsqlCommand(@"SELECT * FROM information_schema.columns WHERE table_schema=@SchemaName AND table_name=@TableName", c.C))
            {
                cmd.Parameters.AddWithValue("@SchemaName", GetDefaultSchema());
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
                CreateTable(c);
                CreateIndexes(c);
            }
            else
            {
                PropertyInfo[] fields = this.GetType().GetProperties();
                foreach (PropertyInfo f in fields)
                {
                    if (IsModelLocal(f))
                        continue;
                    var colName = GetRealDataFieldName(f);

                    using (cmd = new NpgsqlCommand(@"SELECT * FROM information_schema.columns WHERE table_schema=@SchemaName AND table_name=@TableName AND column_name=@ColumnName", c.C))
                    {
                        cmd.Parameters.AddWithValue("@SchemaName", GetDefaultSchema());
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
                        if (exists)
                            continue;
                    }


                    using (cmd = new NpgsqlCommand(@"ALTER TABLE " + GetDefaultSchema() + "." + tableName + " ADD " + colName + " " + GetPropDBType(f) + " " + GetForeignKey(f), c.C))
                    {
                        cmd.ExecuteNonQuery();
                    }
                }

                try
                {
                    CreateIndexes(c);
                }
                catch { }
            }
        }


        public virtual async Task SyncStructureAsync(BaseConnection c)
        {
            string tableName = GetDefaultTable();
            NpgsqlCommand cmd;
            bool exists = false;
            using (cmd = new NpgsqlCommand(@"SELECT * FROM information_schema.columns WHERE table_schema=@SchemaName AND table_name=@TableName", c.C))
            {
                cmd.Parameters.AddWithValue("@SchemaName", GetDefaultSchema());
                cmd.Parameters.AddWithValue("@TableName", tableName);
                using (var r = await cmd.ExecuteReaderAsync())
                {
                    if (await r.ReadAsync())
                    {
                        exists = true;
                    }
                }
            }

            if (!exists)
            {
                await CreateTableAsync(c);
                await CreateIndexesAsync(c);
            }
            else
            {
                PropertyInfo[] fields = this.GetType().GetProperties();
                foreach (PropertyInfo f in fields)
                {
                    if (IsModelLocal(f))
                        continue;
                    var colName = GetRealDataFieldName(f);

                    using (cmd = new NpgsqlCommand(@"SELECT * FROM information_schema.columns WHERE table_schema=@SchemaName AND table_name=@TableName AND column_name=@ColumnName", c.C))
                    {
                        cmd.Parameters.AddWithValue("@SchemaName", GetDefaultSchema());
                        cmd.Parameters.AddWithValue("@TableName", tableName);
                        cmd.Parameters.AddWithValue("@ColumnName", colName);
                        exists = false;
                        using (var r = await cmd.ExecuteReaderAsync())
                        {
                            if (await r.ReadAsync())
                            {
                                exists = true;
                            }
                        }
                        if (exists)
                            continue;
                    }


                    using (cmd = new NpgsqlCommand(@"ALTER TABLE " + GetDefaultSchema() + "." + tableName + " ADD " + colName + " " + GetPropDBType(f) + " " + GetForeignKey(f), c.C))
                    {
                        await cmd.ExecuteNonQueryAsync();
                    }
                }

                try
                {
                    await CreateIndexesAsync(c);
                }
                catch { }
            }
        }

        public BaseModelInfo GetModelInfo()
        {
            var modelInfo = new BaseModelInfo();
            var matt = GetType().GetCustomAttribute<ModelInfo>();
            modelInfo.Title = GetType().Name;
            if (matt != null)
            {
                modelInfo.Title = (matt.Title ?? GetType().Name);
                if (!string.IsNullOrEmpty(matt.CollectionTitle))
                    modelInfo.CollectionTitle = matt.CollectionTitle;
                if (!string.IsNullOrEmpty(matt.Group))
                    modelInfo.Group = matt.Group;
            }
            if (modelInfo.CollectionTitle == null)
                modelInfo.CollectionTitle = modelInfo.Title + " List";
            modelInfo.Name = GetType().Name;
            modelInfo.Variables = new List<ModelVarInfo>();
            PropertyInfo[] fields = this.GetType().GetProperties();
            foreach (PropertyInfo f in fields)
            {
                var colName = GetRealDataFieldName(f);

                var mvi = new ModelVarInfo();

                mvi.Title = f.Name;
                mvi.Name = f.Name;
                mvi.DataName = colName;
                mvi.VarType = f.PropertyType.Name;
                mvi.InputType = mvi.VarType;

                var att = f.GetCustomAttribute<ColVisible>();
                if (att != null)
                {
                    if (!string.IsNullOrEmpty(att.Title))
                        mvi.Title = att.Title;
                    mvi.Note = att.Note;
                    mvi.Listable = att.TableVisible;
                    mvi.Readable = true;
                    mvi.Writable = att.Editable;
                    mvi.Required = att.Required;
                    if (att.Click != null)
                        mvi.Click = att.Click;
                    if (att.LinkedModelType != null)
                        att.LinkedModelName = att.LinkedModelType.Name;
                    if (att.LinkedModelName != null)
                    {
                        mvi.LinkedModelName = att.LinkedModelName;
                        mvi.LinkedModelKey = JsonNamingPolicy.CamelCase.ConvertName(att.LinkedModelKey);
                        mvi.LinkedModelTitle = JsonNamingPolicy.CamelCase.ConvertName(att.LinkedModelTitle);
                        mvi.LinkedModelUnique = att.LinkedModelUnique;
                        if (!string.IsNullOrEmpty(att.LinkedModelSort))
                            mvi.LinkedModelSort = JsonNamingPolicy.CamelCase.ConvertName(att.LinkedModelSort);
                        mvi.InputType = "Model";
                    }
                    if (!string.IsNullOrEmpty(att.CustomInput))
                        mvi.InputType = att.CustomInput;
                }

                modelInfo.Variables.Add(mvi);
            }

            return modelInfo;
        }

        public void CreateForeignKey(BaseConnection db, string columnName, string refTable, string refColumnName, string? fkName = null, bool deleteCascade = false)
        {
            if (fkName == null)
            {
                var rgx = new Regex("[^a-zA-Z-]");
                fkName = rgx.Replace(GetDefaultTable() + "_" + refTable + "_" + refColumnName + "_fkey", "");
            }
            ExecuteCommand(db, "ALTER TABLE IF EXISTS " + GetDefaultTable() + " ADD CONSTRAINT " + fkName.ToLower() + " FOREIGN KEY (" + columnName.ToLower() + ") REFERENCES " + refTable.ToLower() + " (" + refColumnName.ToLower() + ") ON UPDATE NO ACTION " + (deleteCascade ? "ON DELETE CASCADE" : "ON DELETE NO ACTION"));
        }

        public async Task CreateForeignKeyAsync(BaseConnection db, string columnName, string refTable, string refColumnName, string? fkName = null, bool deleteCascade = false)
        {
            if (fkName == null)
            {
                var rgx = new Regex("[^a-zA-Z-]");
                fkName = rgx.Replace(GetDefaultTable() + "_" + refTable + "_" + refColumnName + "_fkey", "");
            }
            await ExecuteCommandAsync(db, "ALTER TABLE IF EXISTS " + GetDefaultTable() + " ADD CONSTRAINT " + fkName.ToLower() + " FOREIGN KEY (" + columnName.ToLower() + ") REFERENCES " + refTable.ToLower() + " (" + refColumnName.ToLower() + ") ON UPDATE NO ACTION " + (deleteCascade ? "ON DELETE CASCADE" : "ON DELETE NO ACTION"));
        }

        public bool TryCreateForeignKey(BaseConnection db, string columnName, string refTable, string refColumnName, string? fkName = null, bool deleteCascade = false)
        {
            try
            {
                CreateForeignKey(db, columnName, refTable, refColumnName, fkName, deleteCascade);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> TryCreateForeignKeyAsync(BaseConnection db, string columnName, string refTable, string refColumnName, string? fkName = null, bool deleteCascade = false)
        {
            try
            {
                await CreateForeignKeyAsync(db, columnName, refTable, refColumnName, fkName, deleteCascade);
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Create index.
        /// </summary>
        /// <param name="db">The BaseConnection object instance.</param>
        /// <param name="indexDefinition">SQL index definition.</param>
        /// <param name="unique">Unique index?</param>
        /// <param name="indexName">SQL index name, if null, it will be auto-generated.</param>
        /// /// <param name="gin">SQL GIN</param>
        public void CreateIndex(BaseConnection db, string indexDefinition, bool unique = false, string? indexName = null, bool gin = false)
        {
            if (indexName == null)
            {
                var rgx = new Regex("[^a-zA-Z-]");
                indexName = rgx.Replace(GetDefaultTable() + "_" + indexDefinition, "");
            }
            ExecuteCommand(db, "CREATE " + (unique ? " UNIQUE " : "") + " INDEX " + indexName + " ON " + GetDefaultTable() + (gin ? " GIN " : " ") + "(" + indexDefinition + ")");
        }

        public async Task CreateIndexAsync(BaseConnection db, string indexDefinition, bool unique = false, string? indexName = null, bool gin = false)
        {
            if (indexName == null)
            {
                var rgx = new Regex("[^a-zA-Z-]");
                indexName = rgx.Replace(GetDefaultTable() + "_" + indexDefinition, "");
            }
            await ExecuteCommandAsync(db, "CREATE " + (unique ? " UNIQUE " : "") + " INDEX " + indexName + " ON " + GetDefaultTable() + (gin ? " GIN " : " ") + "(" + indexDefinition + ")");
        }

        /// <summary>
        /// Try create index.
        /// </summary>
        /// <param name="db">The BaseConnection object instance.</param>
        /// <param name="indexDefinition">SQL index definition.</param>
        /// <param name="unique">Unique index?</param>
        /// <param name="indexName">SQL index name, if null, it will be auto-generated.</param>
        /// <param name="gin">SQL GIN</param>
        /// <returns>true if sucess false if error</returns>
        public bool TryCreateIndex(BaseConnection db, string indexDefinition, bool unique = false, string? indexName = null, bool gin = false)
        {
            try
            {
                CreateIndex(db, indexDefinition, unique, indexName);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> TryCreateIndexAsync(BaseConnection db, string indexDefinition, bool unique = false, string? indexName = null, bool gin = false)
        {
            try
            {
                await CreateIndexAsync(db, indexDefinition, unique, indexName);
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Overridable create indexes function.
        /// </summary>
        public virtual void CreateIndexes(BaseConnection db)
        {
            TryCreateIndex(db, "ID", true);
        }

        public virtual async Task CreateIndexesAsync(BaseConnection db)
        {
            await TryCreateIndexAsync(db, "ID", true);
        }

        /// <summary>
        /// You can use [[DT]] to represent the default table.
        /// </summary>
        /// <param name="db">The BaseConnection object instance.</param>
        /// <param name="query">The SQL query.</param>
        public void ExecuteCommand(BaseConnection db, string query)
        {
            using (var cmd = new NpgsqlCommand(query.Replace("[[DT]]", GetDefaultTable()), db.C))
            {
                cmd.ExecuteNonQuery();
            }
        }

        public async Task ExecuteCommandAsync(BaseConnection db, string query)
        {
            using (var cmd = new NpgsqlCommand(query.Replace("[[DT]]", GetDefaultTable()), db.C))
            {
                await cmd.ExecuteNonQueryAsync();
            }
        }

        /// <summary>
        /// You can use [[DT]] to represent the default table.
        /// </summary>
        /// <param name="db">The BaseConnection object instance.</param>
        /// <param name="query">The SQL query.</param>
        /// <returns>true if sucess false if error</returns>
        public bool TryExecuteCommand(BaseConnection db, string query)
        {
            try
            {
                ExecuteCommand(db, query);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> TryExecuteCommandAsync(BaseConnection db, string query)
        {
            try
            {
                await ExecuteCommandAsync(db, query);
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Prepare string for to_tsquery
        /// </summary>
        /// <param name="query"></param>
        /// <returns>Prepared string.</returns>
        public static string FormatTSQuery(string query)
        {
            string r = string.Empty;
            var regex = new Regex("([a-zA-Z0-9_]+)", RegexOptions.Multiline);
            var matches = regex.Matches(query);
            foreach (Match match in matches)
            {
                if (r.Length > 0)
                    r += " & ";
                r += match.Groups[1].Value;
            }
            return r;
        }
    }

}
