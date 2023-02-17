namespace ReportApp.Utility
{
     public interface ICopyableObject
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
          /// <param name="inputObj"></param>
          void CopyFromOther(object inputObj);

          #endregion Methods
     }
}