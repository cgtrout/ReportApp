using System.Data.Linq.Mapping;

namespace ReportApp.Model
{
     /// <summary>
     /// Description of AccessReprocess.
     ///  - used to tell system what AccessEntries it needs to reload
     /// </summary>
     [Table(Name = "AccessReprocess")]
     public class AccessReprocess
     {
          #region Properties

          [Column(Name = "accessentryid", IsPrimaryKey = true)]
          public long LogId { get; set; }

          #endregion Properties
     }
}