namespace DataValidation.Main
{
  using System.Collections.Generic;
  using System.Linq;

  public class ValidationResult
  {
    public ValidationResult(bool isValild, object errorMessage)
      : this(isValild, new[] { errorMessage })
    {
    }

    public ValidationResult(bool isValid, IEnumerable<object> errorMessages)
    {
      this.IsValid = isValid;
      this.ErrorMessages = errorMessages;
    }

    public static ValidationResult ValidResult { get; } 
      = new ValidationResult(true, Enumerable.Empty<object>());

    public bool IsValid { get; }
    public IEnumerable<object> ErrorMessages { get; }
  }
}
