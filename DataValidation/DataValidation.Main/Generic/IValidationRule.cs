namespace DataValidation.Main.Generic
{
  using System.Globalization;

  public interface IValidationRule<TValue> : IValidationRule
  {
    ValidationResult Validate(TValue value, CultureInfo cultureInfo);

#if NET
    ValidationResult Validate(TValue value)
      => Validate(value, CultureInfo.CurrentCulture);
#endif
  }
}
