using System.Web.Security;
using System.Configuration.Provider;
using System.Collections.Specialized;
using System;
using MySql.Data.MySqlClient;
using System.Configuration;
using System.Diagnostics;
using System.Web;
using System.Globalization;

/*

-- Please, send me an email (andriniaina@gmail.com) if you have done some improvements or bug corrections to this file


CREATE TABLE Roles
(
  Rolename Varchar (255) NOT NULL,
  ApplicationName varchar (255) NOT NULL
)

CREATE TABLE UsersInRoles
(
  Username Varchar (255) NOT NULL,
  Rolename Varchar (255) NOT NULL,
  ApplicationName Text (255) NOT NULL
)
ALTER TABLE `usersinroles` ADD INDEX ( `Username` , `Rolename` , `ApplicationName` ) ;
ALTER TABLE `roles` ADD INDEX ( `Rolename` , `ApplicationName` ) ;

*/


namespace Andri.Web
{

    public sealed class MySqlRoleProvider : RoleProvider
    {

        //
        // Global connection string, generic exception message, event log info.
        //

        private const string rolesTable = "Roles";
        private const string usersInRolesTable = "UsersInRoles";

        private string eventSource = "MySqlRoleProvider";
        private string eventLog = "Application";
        private string exceptionMessage = "An exception occurred. Please check the Event Log.";

        private ConnectionStringSettings pConnectionStringSettings;
        private string connectionString;


        //
        // If false, exceptions are thrown to the caller. If true,
        // exceptions are written to the event log.
        //

        private bool pWriteExceptionsToEventLog = false;

        public bool WriteExceptionsToEventLog
        {
            get { return pWriteExceptionsToEventLog; }
            set { pWriteExceptionsToEventLog = value; }
        }



        //
        // System.Configuration.Provider.ProviderBase.Initialize Method
        //

        public override void Initialize(string name, NameValueCollection config)
        {

            //
            // Initialize values from web.config.
            //

            if (config == null)
                throw new ArgumentNullException("config");

            if (name == null || name.Length == 0)
                name = "MySqlRoleProvider";

            if (String.IsNullOrEmpty(config["description"]))
            {
                config.Remove("description");
                config.Add("description", "Sample MySql Role provider");
            }

            // Initialize the abstract base class.
            base.Initialize(name, config);


            if (config["applicationName"] == null || config["applicationName"].Trim().Length == 0)
            {
                pApplicationName = System.Web.Hosting.HostingEnvironment.ApplicationVirtualPath;
            }
            else
            {
                pApplicationName = config["applicationName"];
            }


            if (config["writeExceptionsToEventLog"] != null)
            {
                if (config["writeExceptionsToEventLog"].ToUpper(CultureInfo.InvariantCulture) == "TRUE")
                {
                    pWriteExceptionsToEventLog = true;
                }
            }


            //
            // Initialize MySqlConnection.
            //

            pConnectionStringSettings = ConfigurationManager.
              ConnectionStrings[config["connectionStringName"]];

            if (pConnectionStringSettings == null || pConnectionStringSettings.ConnectionString.Trim().Length == 0)
            {
                throw new ProviderException("Connection string cannot be blank.");
            }

            connectionString = pConnectionStringSettings.ConnectionString;
        }



        //
        // System.Web.Security.RoleProvider properties.
        //


        private string pApplicationName;


        public override string ApplicationName
        {
            get { return pApplicationName; }
            set { pApplicationName = value; }
        }

        //
        // System.Web.Security.RoleProvider methods.
        //

        //
        // RoleProvider.AddUsersToRoles
        //

        public override void AddUsersToRoles(string[] usernames, string[] roleNames)
        {
            if (usernames == null)
                throw new ArgumentNullException("usernames");
            if (roleNames == null)
                throw new ArgumentNullException("roleNames");
            foreach (string rolename in roleNames)
            {
                if (!RoleExists(rolename))
                {
                    throw new ProviderException("Role name not found.");
                }
            }

            foreach (string username in usernames)
            {
                if (username.IndexOf(',') > 0)
                {
                    throw new ArgumentException("User names cannot contain commas.");
                }

                foreach (string rolename in roleNames)
                {
                    if (IsUserInRole(username, rolename))
                    {
                        throw new ProviderException("User is already in role.");
                    }
                }
            }


            using (MySqlConnection conn = new MySqlConnection(connectionString))
            {
                using (MySqlCommand cmd = new MySqlCommand("INSERT INTO `" + usersInRolesTable + "`" +
                        " (Username, Rolename, ApplicationName) " +
                        " Values(?Username, ?Rolename, ?ApplicationName)", conn))
                {

                    MySqlParameter userParm = cmd.Parameters.Add("?Username", MySqlDbType.VarChar, 255);
                    MySqlParameter roleParm = cmd.Parameters.Add("?Rolename", MySqlDbType.VarChar, 255);
                    cmd.Parameters.Add("?ApplicationName", MySqlDbType.VarChar, 255).Value = ApplicationName;

                    MySqlTransaction tran = null;

                    try
                    {
                        conn.Open();
                        tran = conn.BeginTransaction();
                        cmd.Transaction = tran;

                        foreach (string username in usernames)
                        {
                            foreach (string rolename in roleNames)
                            {
                                userParm.Value = username;
                                roleParm.Value = rolename;
                                cmd.ExecuteNonQuery();
                            }
                        }

                        tran.Commit();
                    }
                    catch (MySqlException e)
                    {
                        try
                        {
                            tran.Rollback();
                        }
                        catch (MySqlException) { }


                        if (WriteExceptionsToEventLog)
                        {
                            WriteToEventLog(e, "AddUsersToRoles");
                        }
                        else
                        {
                            throw;
                        }
                    }
                }
            }
        }


        //
        // RoleProvider.CreateRole
        //

        public override void CreateRole(string roleName)
        {
            if (roleName == null)
                throw new ArgumentNullException("roleName");
            if (roleName.IndexOf(',') > 0)
            {
                throw new ArgumentException("Role names cannot contain commas.");
            }

            if (RoleExists(roleName))
            {
                throw new ProviderException("Role name already exists.");
            }

            using (MySqlConnection conn = new MySqlConnection(connectionString))
            {
                using (MySqlCommand cmd = new MySqlCommand("INSERT INTO `" + rolesTable + "`" +
                        " (Rolename, ApplicationName) " +
                        " Values(?Rolename, ?ApplicationName)", conn))
                {

                    cmd.Parameters.Add("?Rolename", MySqlDbType.VarChar, 255).Value = roleName;
                    cmd.Parameters.Add("?ApplicationName", MySqlDbType.VarChar, 255).Value = ApplicationName;

                    try
                    {
                        conn.Open();

                        cmd.ExecuteNonQuery();
                    }
                    catch (MySqlException e)
                    {
                        if (WriteExceptionsToEventLog)
                        {
                            WriteToEventLog(e, "CreateRole");
                        }
                        else
                        {
                            throw;
                        }
                    }
                }
            }
        }


        //
        // RoleProvider.DeleteRole
        //

        public override bool DeleteRole(string roleName, bool throwOnPopulatedRole)
        {
            if (!RoleExists(roleName))
            {
                throw new ProviderException("Role does not exist.");
            }

            if (throwOnPopulatedRole && GetUsersInRole(roleName).Length > 0)
            {
                throw new ProviderException("Cannot delete a populated role.");
            }

            using (MySqlConnection conn = new MySqlConnection(connectionString))
            {
                using (MySqlCommand cmd = new MySqlCommand("DELETE FROM `" + rolesTable + "`" +
                        " WHERE Rolename = ?Rolename AND ApplicationName = ?ApplicationName", conn))
                {

                    cmd.Parameters.Add("?Rolename", MySqlDbType.VarChar, 255).Value = roleName;
                    cmd.Parameters.Add("?ApplicationName", MySqlDbType.VarChar, 255).Value = ApplicationName;


                    using (MySqlCommand cmd2 = new MySqlCommand("DELETE FROM `" + usersInRolesTable + "`" +
                            " WHERE Rolename = ?Rolename AND ApplicationName = ?ApplicationName", conn))
                    {

                        cmd2.Parameters.Add("?Rolename", MySqlDbType.VarChar, 255).Value = roleName;
                        cmd2.Parameters.Add("?ApplicationName", MySqlDbType.VarChar, 255).Value = ApplicationName;

                        MySqlTransaction tran = null;

                        try
                        {
                            conn.Open();
                            tran = conn.BeginTransaction();
                            cmd.Transaction = tran;
                            cmd2.Transaction = tran;

                            cmd2.ExecuteNonQuery();
                            cmd.ExecuteNonQuery();

                            tran.Commit();
                        }
                        catch (MySqlException e)
                        {
                            try
                            {
                                tran.Rollback();
                            }
                            catch (MySqlException) { }


                            if (WriteExceptionsToEventLog)
                            {
                                WriteToEventLog(e, "DeleteRole");

                                return false;
                            }
                            else
                            {
                                throw;
                            }
                        }
                    }
                }
            }

            return true;
        }


        //
        // RoleProvider.GetAllRoles
        //

        public override string[] GetAllRoles()
        {
            string tmpRoleNames = "";

            using (MySqlConnection conn = new MySqlConnection(connectionString))
            {
                using (MySqlCommand cmd = new MySqlCommand("SELECT Rolename FROM `" + rolesTable + "`" +
                          " WHERE ApplicationName = ?ApplicationName", conn))
                {

                    cmd.Parameters.Add("?ApplicationName", MySqlDbType.VarChar, 255).Value = ApplicationName;

                    MySqlDataReader reader = null;

                    try
                    {
                        conn.Open();

                        using (reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                tmpRoleNames += reader.GetString(0) + ",";
                            }
                        }
                    }
                    catch (MySqlException e)
                    {
                        if (WriteExceptionsToEventLog)
                        {
                            WriteToEventLog(e, "GetAllRoles");
                        }
                        else
                        {
                            throw;
                        }
                    }

                    if (tmpRoleNames.Length > 0)
                    {
                        // Remove trailing comma.
                        tmpRoleNames = tmpRoleNames.Substring(0, tmpRoleNames.Length - 1);
                        return tmpRoleNames.Split(',');
                    }
                }
            }

            return new string[0];
        }


        //
        // RoleProvider.GetRolesForUser
        //

        public override string[] GetRolesForUser(string username)
        {
            string tmpRoleNames = "";

            using (MySqlConnection conn = new MySqlConnection(connectionString))
            {
                using (MySqlCommand cmd = new MySqlCommand("SELECT Rolename FROM `" + usersInRolesTable + "`" +
                        " WHERE Username = ?Username AND ApplicationName = ?ApplicationName", conn))
                {

                    cmd.Parameters.Add("?Username", MySqlDbType.VarChar, 255).Value = username;
                    cmd.Parameters.Add("?ApplicationName", MySqlDbType.VarChar, 255).Value = ApplicationName;

                    MySqlDataReader reader = null;

                    try
                    {
                        conn.Open();

                        using (reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                tmpRoleNames += reader.GetString(0) + ",";
                            }
                        }
                    }
                    catch (MySqlException e)
                    {
                        if (WriteExceptionsToEventLog)
                        {
                            WriteToEventLog(e, "GetRolesForUser");
                        }
                        else
                        {
                            throw;
                        }
                    }

                    if (tmpRoleNames.Length > 0)
                    {
                        // Remove trailing comma.
                        tmpRoleNames = tmpRoleNames.Substring(0, tmpRoleNames.Length - 1);
                        return tmpRoleNames.Split(',');
                    }
                }
            }

            return new string[0];
        }


        //
        // RoleProvider.GetUsersInRole
        //

        public override string[] GetUsersInRole(string roleName)
        {
            string tmpUserNames = "";

            using (MySqlConnection conn = new MySqlConnection(connectionString))
            {
                using (MySqlCommand cmd = new MySqlCommand("SELECT Username FROM `" + usersInRolesTable + "`" +
                          " WHERE Rolename = ?Rolename AND ApplicationName = ?ApplicationName", conn))
                {
                    cmd.Parameters.Add("?Rolename", MySqlDbType.VarChar, 255).Value = roleName;
                    cmd.Parameters.Add("?ApplicationName", MySqlDbType.VarChar, 255).Value = ApplicationName;

                    MySqlDataReader reader = null;

                    try
                    {
                        conn.Open();

                        using (reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                tmpUserNames += reader.GetString(0) + ",";
                            }
                        }
                    }
                    catch (MySqlException e)
                    {
                        if (WriteExceptionsToEventLog)
                        {
                            WriteToEventLog(e, "GetUsersInRole");
                        }
                        else
                        {
                            throw;
                        }
                    }
                }
            }

            if (tmpUserNames.Length > 0)
            {
                // Remove trailing comma.
                tmpUserNames = tmpUserNames.Substring(0, tmpUserNames.Length - 1);
                return tmpUserNames.Split(',');
            }

            return new string[0];
        }


        //
        // RoleProvider.IsUserInRole
        //

        public override bool IsUserInRole(string username, string roleName)
        {
            bool userIsInRole = false;

            using (MySqlConnection conn = new MySqlConnection(connectionString))
            {
                using (MySqlCommand cmd = new MySqlCommand("SELECT COUNT(*) FROM `" + usersInRolesTable + "`" +
                        " WHERE Username = ?Username AND Rolename = ?Rolename AND ApplicationName = ?ApplicationName", conn))
                {
                    cmd.Parameters.Add("?Username", MySqlDbType.VarChar, 255).Value = username;
                    cmd.Parameters.Add("?Rolename", MySqlDbType.VarChar, 255).Value = roleName;
                    cmd.Parameters.Add("?ApplicationName", MySqlDbType.VarChar, 255).Value = ApplicationName;

                    try
                    {
                        conn.Open();

                        long numRecs = Convert.ToInt64(cmd.ExecuteScalar(), CultureInfo.InvariantCulture);

                        if (numRecs > 0)
                        {
                            userIsInRole = true;
                        }
                    }
                    catch (MySqlException e)
                    {
                        if (WriteExceptionsToEventLog)
                        {
                            WriteToEventLog(e, "IsUserInRole");
                        }
                        else
                        {
                            throw;
                        }
                    }
                }
            }

            return userIsInRole;
        }


        //
        // RoleProvider.RemoveUsersFromRoles
        //

        public override void RemoveUsersFromRoles(string[] usernames, string[] roleNames)
        {
            if (usernames == null)
                throw new ArgumentNullException("usernames");
            if (roleNames == null)
                throw new ArgumentNullException("roleNames");

            foreach (string rolename in roleNames)
            {
                if (!RoleExists(rolename))
                {
                    throw new ProviderException("Role name not found.");
                }
            }

            foreach (string username in usernames)
            {
                foreach (string rolename in roleNames)
                {
                    if (!IsUserInRole(username, rolename))
                    {
                        throw new ProviderException("User is not in role.");
                    }
                }
            }


            using (MySqlConnection conn = new MySqlConnection(connectionString))
            {
                using (MySqlCommand cmd = new MySqlCommand("DELETE FROM `" + usersInRolesTable + "`" +
                        " WHERE Username = ?Username AND Rolename = ?Rolename AND ApplicationName = ?ApplicationName", conn))
                {

                    MySqlParameter userParm = cmd.Parameters.Add("?Username", MySqlDbType.VarChar, 255);
                    MySqlParameter roleParm = cmd.Parameters.Add("?Rolename", MySqlDbType.VarChar, 255);
                    cmd.Parameters.Add("?ApplicationName", MySqlDbType.VarChar, 255).Value = ApplicationName;

                    MySqlTransaction tran = null;

                    try
                    {
                        conn.Open();
                        tran = conn.BeginTransaction();
                        cmd.Transaction = tran;

                        foreach (string username in usernames)
                        {
                            foreach (string rolename in roleNames)
                            {
                                userParm.Value = username;
                                roleParm.Value = rolename;
                                cmd.ExecuteNonQuery();
                            }
                        }

                        tran.Commit();
                    }
                    catch (MySqlException e)
                    {
                        try
                        {
                            tran.Rollback();
                        }
                        catch (MySqlException) { }


                        if (WriteExceptionsToEventLog)
                        {
                            WriteToEventLog(e, "RemoveUsersFromRoles");
                        }
                        else
                        {
                            throw;
                        }
                    }
                }
            }
        }


        //
        // RoleProvider.RoleExists
        //

        public override bool RoleExists(string roleName)
        {
            bool exists = false;

            using (MySqlConnection conn = new MySqlConnection(connectionString))
            {
                using (MySqlCommand cmd = new MySqlCommand("SELECT COUNT(*) FROM `" + rolesTable + "`" +
                          " WHERE Rolename = ?Rolename AND ApplicationName = ?ApplicationName", conn))
                {

                    cmd.Parameters.Add("?Rolename", MySqlDbType.VarChar, 255).Value = roleName;
                    cmd.Parameters.Add("?ApplicationName", MySqlDbType.VarChar, 255).Value = ApplicationName;

                    try
                    {
                        conn.Open();

                        long numRecs = Convert.ToInt64(cmd.ExecuteScalar(), CultureInfo.InvariantCulture);

                        if (numRecs > 0)
                        {
                            exists = true;
                        }
                    }
                    catch (MySqlException e)
                    {
                        if (WriteExceptionsToEventLog)
                        {
                            WriteToEventLog(e, "RoleExists");
                        }
                        else
                        {
                            throw;
                        }
                    }
                }
            }

            return exists;
        }

        //
        // RoleProvider.FindUsersInRole
        //

        public override string[] FindUsersInRole(string roleName, string usernameToMatch)
        {
            using (MySqlConnection conn = new MySqlConnection(connectionString))
            {
                using (MySqlCommand cmd = new MySqlCommand("SELECT Username FROM `" + usersInRolesTable + "` " +
                          "WHERE Username LIKE ?UsernameSearch AND Rolename = ?Rolename AND ApplicationName = ?ApplicationName", conn))
                {
                    cmd.Parameters.Add("?UsernameSearch", MySqlDbType.VarChar, 255).Value = usernameToMatch;
                    cmd.Parameters.Add("?RoleName", MySqlDbType.VarChar, 255).Value = roleName;
                    cmd.Parameters.Add("?ApplicationName", MySqlDbType.VarChar, 255).Value = pApplicationName;

                    string tmpUserNames = "";
                    MySqlDataReader reader = null;

                    try
                    {
                        conn.Open();

                        using (reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                tmpUserNames += reader.GetString(0) + ",";
                            }
                        }
                    }
                    catch (MySqlException e)
                    {
                        if (WriteExceptionsToEventLog)
                        {
                            WriteToEventLog(e, "FindUsersInRole");
                        }
                        else
                        {
                            throw;
                        }
                    }

                    if (tmpUserNames.Length > 0)
                    {
                        // Remove trailing comma.
                        tmpUserNames = tmpUserNames.Substring(0, tmpUserNames.Length - 1);
                        return tmpUserNames.Split(',');
                    }
                }
            }

            return new string[0];
        }

        //
        // WriteToEventLog
        //   A helper function that writes exception detail to the event log. Exceptions
        // are written to the event log as a security measure to avoid private database
        // details from being returned to the browser. If a method does not return a status
        // or boolean indicating the action succeeded or failed, a generic exception is also 
        // thrown by the caller.
        //
        private void WriteToEventLog(MySqlException e, string action)
        {
            using (EventLog log = new EventLog())
            {
                log.Source = eventSource;
                log.Log = eventLog;

                string message = exceptionMessage + "\n\n";
                message += Resources.LocalizedText.LogAction + action + "\n\n";
                message += Resources.LocalizedText.LogException + e.ToString();

                log.WriteEntry(message);
            }
        }

    }
}