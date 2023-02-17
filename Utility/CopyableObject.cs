namespace ReportApp.Utility
{
     public interface CopyableObject
     {
          #region Methods

          /// <summary>
          /// Get copy of this object
          /// </summary>
          /// <returns></returns>
          object Copy();

          /// <summary>
          /// Copies values from obj into this object
          /// </summary>
          /// <param name="obj"></param>
          void CopyFromOther(object obj);

          #endregion Methods
     }
}