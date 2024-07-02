namespace DataValidation.Main
{
  using System.Globalization;

  public interface IValidationRule
  {
    ValidationResult Validate(object value, CultureInfo cultureInfo);
#if NET
    ValidationResult Validate(object value)
      => Validate(value, CultureInfo.CurrentCulture);
#endif
  }
}
