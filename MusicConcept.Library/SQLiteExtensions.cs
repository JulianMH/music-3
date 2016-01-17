using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MusicConcept.Library
{
    static class SQLiteExtensions
    {
        public static async Task<bool> TryRunInTransactionAsync(this SQLiteAsyncConnection databaseConnection, Action<SQLiteConnection> action)
        {
            try
            {
                await databaseConnection.RunInTransactionAsync(action);
                return true;
            }
            catch (SQLiteException exception)
            {
                if (exception.Message == "Busy")
                    return false;
                else
                    throw exception;
            }
        }
    }
}
