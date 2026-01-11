using System;
using Microsoft.Data.Sqlite;

namespace WinPOS.Data
{
    public static class SettingsStore
    {
        public static string Get(string key, string defaultValue)
        {
            using var con = PosDb.Open();
            using var cmd = con.CreateCommand();
            cmd.CommandText = "SELECT Value FROM Settings WHERE Key=@k;";
            cmd.Parameters.AddWithValue("@k", key);

            var v = cmd.ExecuteScalar();
            return v == null ? defaultValue : Convert.ToString(v) ?? defaultValue;
        }

        public static void Set(string key, string value)
        {
            using var con = PosDb.Open();
            using var cmd = con.CreateCommand();
            cmd.CommandText = @"
INSERT INTO Settings(Key, Value) VALUES(@k, @v)
ON CONFLICT(Key) DO UPDATE SET Value=@v;";
            cmd.Parameters.AddWithValue("@k", key);
            cmd.Parameters.AddWithValue("@v", value);
            cmd.ExecuteNonQuery();
        }
    }
}
