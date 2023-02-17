using System.Data.Linq.Mapping;

namespace ReportApp.Model
{
     /// <summary>
     /// Description of Parameter.
     /// </summary>
     [Table(Name = "parameter")]
     public class Parameter
     {
          #region Properties

          [Column(Name = "name")]
          public string Name { get; set; }

          [Column(Name = "parameterid", IsPrimaryKey = true)]
          public string PersonId { get; set; }

          [Column(Name = "value")]
          public string Value { get; set; }

          #endregion Properties
     }
}