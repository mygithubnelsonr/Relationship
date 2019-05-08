using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;

namespace Relationship
{
    class Program
    {
        /// <summary>
        /// Datenstruktur für die Beziehungen (TabelleA nach TabelleB)
        /// </summary>
        public struct Relationship
        {
            public String tableA;
            public String tableB;
        }
        static void Main(string[] args)
        {

            try
            {
                SqlConnection dbConnection = new SqlConnection
                 (@"data source=xxxxx;Integrated Security=SSPI;initial catalog=xxxx;persist security info=false;packet size=4096");
                dbConnection.Open();
                List<String> tables = GetTableNames(dbConnection);
                List<Relationship> rels = GetRelationsships(dbConnection);
                SaveGml(TablesToGml(tables), RelationshipsToGml(rels));
                Console.WriteLine("Die Datei wurde erfolgreich erzeugt");

            }
            catch (Exception e)
            {
                Console.Write(e.Message);
            }
            Console.ReadLine();
        }

        /// <summary>
        /// Speichert die endgültige GML-Datei ab.
        /// </summary>
        /// <param name="tables"></param>
        /// <param name="rels"></param>
        private static void SaveGml(string tables, string rels)
        {
            File.WriteAllText("test.gml",
                @"Creator   ""yFiles""
    Version ""2.6""
    graph
    [
        hierarchic  1
        label   """"
        directed    1" + tables + rels + "]");
        }

        /// <summary>
        /// Wandelt die Liste der Tabelle ins (stark vereinfachte) GML-Format um.
        /// </summary>
        /// <param name="tables"></param>
        /// <returns></returns>
        public static String TablesToGml(List<String> tables)
        {
            String gml = "";
            foreach (string tab in tables)
            {
                gml = String.Concat(gml, Environment.NewLine,
                     "node", Environment.NewLine,
                     "[", Environment.NewLine,
                     "id \"" + tab + "\"", Environment.NewLine,
                     "label  \"" + tab + "\"", Environment.NewLine,
                     "]");
            }
            return gml;
        }

        /// <summary>
        /// Wandelt die Liste der Beziehungen ins (stark vereinfachte) GML-Format um
        /// </summary>
        /// <param name="rels"></param>
        /// <returns></returns>
        public static String RelationshipsToGml(List<Relationship> rels)
        {
            String gml = "";
            foreach (Relationship rel in rels)
            {
                gml = String.Concat(gml, Environment.NewLine,
                    "edge", Environment.NewLine,
                    "[", Environment.NewLine,
                    "   source \"" + rel.tableA + "\"", Environment.NewLine,
                    "   target \"" + rel.tableB + "\"", Environment.NewLine,
                    "]");
            }
            return gml;
        }

        /// <summary>
        /// Liest die Namen der Tabellen aus der Datenbank
        /// </summary>
        /// <param name="dbConnection"></param>
        /// <returns></returns>
        public static List<String> GetTableNames(SqlConnection dbConnection)
        {
            List<string> tables = new List<string>();
            SqlCommand sqlCommand = dbConnection.CreateCommand();
            sqlCommand.CommandText =
                @"Select Name from dbo.sysobjects WHERE xType = 'U' order by Name";
            SqlDataReader sqlReader = sqlCommand.ExecuteReader();
            while (sqlReader.Read())
            {
                tables.Add(sqlReader["Name"].ToString());
            }
            sqlReader.Close();
            return tables;
        }

        /// <summary>
        /// Liest die Relationsships aus der Datenbank aus
        /// </summary>
        /// <param name="dbConnection"></param>
        /// <returns></returns>
        public static List<Relationship> GetRelationsships(SqlConnection dbConnection)
        {
            List<Relationship> rel = new List<Relationship>();
            SqlCommand sqlCommand = dbConnection.CreateCommand();
            sqlCommand.CommandText =
                @"SELECT
                    K_Table = FK.TABLE_NAME,
                    FK_Column = CU.COLUMN_NAME,
                    PK_Table = PK.TABLE_NAME,
                    PK_Column = PT.COLUMN_NAME,
                    Constraint_Name = C.CONSTRAINT_NAME
                    FROM INFORMATION_SCHEMA.REFERENTIAL_CONSTRAINTS C
                    INNER JOIN INFORMATION_SCHEMA.TABLE_CONSTRAINTS FK ON C.CONSTRAINT_NAME = FK.CONSTRAINT_NAME
                    INNER JOIN INFORMATION_SCHEMA.TABLE_CONSTRAINTS PK ON C.UNIQUE_CONSTRAINT_NAME = PK.CONSTRAINT_NAME
                    INNER JOIN INFORMATION_SCHEMA.KEY_COLUMN_USAGE CU ON C.CONSTRAINT_NAME = CU.CONSTRAINT_NAME
                    INNER JOIN (
                    SELECT i1.TABLE_NAME, i2.COLUMN_NAME
                    FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS i1
                    INNER JOIN INFORMATION_SCHEMA.KEY_COLUMN_USAGE i2 ON i1.CONSTRAINT_NAME = i2.CONSTRAINT_NAME
                    WHERE i1.CONSTRAINT_TYPE = 'PRIMARY KEY'
                    ) PT ON PT.TABLE_NAME = PK.TABLE_NAME";
            SqlDataReader sqlReader = sqlCommand.ExecuteReader();
            while (sqlReader.Read())
            {
                Relationship singleRel = new Relationship();
                singleRel.tableA = sqlReader["K_Table"].ToString();
                singleRel.tableB = sqlReader["PK_Table"].ToString();

                //Gibt's mehrere gleiche Beziehungen wird nur EINE dargestellt.
                if (!rel.Contains(singleRel))
                {
                    rel.Add(singleRel);
                }

            }
            sqlReader.Close();
            return rel;

        }
    }

}