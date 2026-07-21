//EmployeeViewModel class added by the syncfusion
using ASMED.WPF.Helpers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ASMED.WPF.ViewModels
{
    public class AutoCompleteViewModel
    {
        public List<string> GetImiona()
        {
            var db = new AccessDbHelper();
            var imiona = new List<string>();
            using (var conn = db.GetConnection())
            {
                conn.Open();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "SELECT S_imie FROM S_Imiona";
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            imiona.Add(reader.GetString(0));
                        }
                    }
                }
            }
            return imiona;
            // debug

        }
        // INotifyPropertyChanged implementacja...
    }

}


