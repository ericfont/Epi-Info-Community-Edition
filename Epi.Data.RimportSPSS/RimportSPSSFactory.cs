using System;
using System.Data;
using System.Data.SqlClient;
using System.Collections.Generic;
using System.Text;
using System.Data.Common;
using Epi.Data.RimportSPSS.Forms;
using System.IO;
using Epi.Data.RimportSPSS;
using Epi;
using System.Windows.Forms;

namespace Epi.Data.RimportSPSS
{
    /// <summary>
    /// RimportSPSSColumnType - Database Factory for RimportSPSS Databases
    /// </summary>
    public class RimportSPSSFactory : IDbDriverFactory
    {

        private RimportSPSSConnectionStringBuilder RimportSPSSConnBuild = new RimportSPSSConnectionStringBuilder();

        public bool ArePrerequisitesMet()
        {
            return true;  // This might be a good place to check to see if R present.
        }

        public string PrerequisiteMessage
        {
            get { return string.Empty; }
        }

        #region IDbDriverFactory Members

        /// <summary>
        /// Create a database
        /// </summary>
        /// <param name="dbInfo">DbDriverInfo</param>
        public void CreatePhysicalDatabase(DbDriverInfo dbInfo)
        {
            RimportSPSSConnectionStringBuilder masterBuilder = new RimportSPSSConnectionStringBuilder(dbInfo.DBCnnStringBuilder.ToString());
            RimportSPSSConnectionStringBuilder tempBuilder = new RimportSPSSConnectionStringBuilder(dbInfo.DBCnnStringBuilder.ToString());

            //tempBuilder = dbInfo.DBCnnStringBuilder as RimportSPSSConnectionStringBuilder;
            //The "test" database is installed by default with RimportSPSS.  System needs to login to this database to create a new database.
            tempBuilder.Database = "information_schema"; 
            
            RimportSPSSConnection masterConnection = new RimportSPSSConnection(tempBuilder.ToString());
            
            try
            {
                RimportSPSSCommand command = masterConnection.CreateCommand() as RimportSPSSCommand;
                if(dbInfo.DBName != null)
                {
                    command.CommandText = "create database " + dbInfo.DBName + ";";
                }
                
                masterConnection.Open();
                //Logger.Log(command.CommandText);
                command.ExecuteNonQuery();

                //reset database to new database for correct storage of meta tables
                tempBuilder.Database = dbInfo.DBName;
            }
            catch (Exception ex)
            {
                throw new System.ApplicationException("Could not create new RimportSPSS Database", ex);//(Epi.SharedStrings.CAN_NOT_CREATE_NEW_MYSQL, ex); 
            }
            finally
            {
                masterConnection.Close();
            }
        }

        /// <summary>
        /// Create an instance of database object
        /// </summary>
        /// <param name="connectionStringBuilder">A connection string builder which contains the connection string</param>
        /// <returns>IDbDriver instance</returns>
        public IDbDriver CreateDatabaseObject(System.Data.Common.DbConnectionStringBuilder connectionStringBuilder)
        {
            IDbDriver instance = new RimportSPSSDatabase();
            instance.ConnectionString = connectionStringBuilder.ConnectionString;
            
            return instance;
        }

        /// <summary>
        /// Create a database with a name that has already been established
        /// </summary>
        /// <param name="configDatabaseKey">Name of the database</param>
        /// <returns>IDbDriver instance</returns>
        public IDbDriver CreateDatabaseObjectByConfiguredName(string configDatabaseKey)
        {
            //may not use since PHIN is .MDB
            IDbDriver instance = null;
            Configuration config = Configuration.GetNewInstance();
            DataRow[] result = config.DatabaseConnections.Select("Name='" + configDatabaseKey + "'");
            if (result.Length == 1)
            {
                Epi.DataSets.Config.DatabaseRow dbConnection = (Epi.DataSets.Config.DatabaseRow)result[0];
                RimportSPSSConnectionStringBuilder RimportSPSSConnectionBuilder = new RimportSPSSConnectionStringBuilder(dbConnection.ConnectionString);
                instance = CreateDatabaseObject(RimportSPSSConnectionBuilder);
            }
            else
            {
                throw new GeneralException("Database name is not configured.");
            }

            return instance;
        }

        /// <summary>
        /// Launch GUI for connection string for an existing database  Throws NotSupportedException if there is 
        /// no GUI associated with the current environment.
        /// </summary>
        /// <returns>IConnectionStringGui</returns>
        public IConnectionStringGui GetConnectionStringGuiForExistingDb()
        {
            if (Configuration.Environment == ExecutionEnvironment.WindowsApplication)
            {
                return new SPSSConnectionStringDialog();
            }
            else
            {
                throw new NotSupportedException("No GUI associated with current environment.");
            }
        }

        /// <summary>
        /// Launch GUI for connection string for a new database  Throws NotSupportedException if there is 
        /// no GUI associated with the current environment.
        /// </summary>
        /// <returns>IConnectionStringGui</returns>
        public IConnectionStringGui GetConnectionStringGuiForNewDb()
        {
            
            if (Configuration.Environment == ExecutionEnvironment.WindowsApplication)
            {
                return new NonExistingConnectionStringDialog();
               
            }
            else
            {
                throw new NotSupportedException("No GUI associated with current environment.");
            }
        }

        /// <summary>
        /// Get a new connection, given a fileName
        /// </summary>
        /// <param name="fileName">Name of the file to become the connectionString</param>
        /// <returns>System.Data.Common.DbConnectionStringBuilder</returns>
        public System.Data.Common.DbConnectionStringBuilder RequestNewConnection(string fileName)
        {
            DbConnectionStringBuilder dbStringBuilder = new DbConnectionStringBuilder(false);
            //dbStringBuilder.ConnectionString = fileName;

            return dbStringBuilder;
        }

        /// <summary>
        /// Get the default connection
        /// </summary>
        /// <param name="databaseName">Name of the database to get the default connection from</param>
        /// <returns></returns>
        public System.Data.Common.DbConnectionStringBuilder RequestDefaultConnection(string databaseName, string projectName = "")
        {
            DbConnectionStringBuilder dbStringBuilder = new DbConnectionStringBuilder(false);
            dbStringBuilder.ConnectionString = RimportSPSSDatabase.BuildDefaultConnectionString(databaseName);
            return dbStringBuilder;
        }

        /// <summary>
        /// Default RimportSPSS ConnectionString request.
        /// </summary>
        /// <param name="database">Data store.</param>
        /// <param name="server">Server location of database.</param>
        /// <param name="user">User account login Id.</param>
        /// <param name="password">User account password.</param>
        /// <returns>Strongly typed connection string builder.</returns>
        public System.Data.Common.DbConnectionStringBuilder RequestDefaultConnection(string database, string server, string user, string password)
        {
            RimportSPSSConnBuild.AutoCache = false; //.PersistSecurityInfo = false;
            RimportSPSSConnBuild.Database = database;
            RimportSPSSConnBuild.Server = server;
            RimportSPSSConnBuild.User = user;
            RimportSPSSConnBuild.Password = password;

            return (RimportSPSSConnBuild as DbConnectionStringBuilder); 
        }

        public bool CanClaimConnectionString(string connectionString)
        {
            string conn = connectionString.ToLowerInvariant();
            if (conn.Contains("RimportSPSS"))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        #endregion

        public string ConvertFileStringToConnectionString(string fileString)
        {
            return fileString;
        }

        public string GetCreateFromDataTableSQL(string tableName, DataTable table)
        {
            throw new NotImplementedException();
        }

        public string SQLGetType(object type, int columnSize, int numericPrecision, int numericScale)
        {
            throw new NotImplementedException();
        }
    }
}