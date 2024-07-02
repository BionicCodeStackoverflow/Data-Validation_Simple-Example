namespace DataValidation.Main
{
  using System.Globalization;

  public class UserInputValidationRule : ValidationRule<string>
  {
    public UserInputValidationRule()
    {
    }

    public UserInputValidationRule(CultureInfo culture) : base(culture)
    {
    }

    public override ValidationResult Validate(string value, CultureInfo cultureInfo)
      => value.StartsWith("@")
        ? ValidationResult.ValidResult
        : new ValidationResult(false, "Input must start with '@'.");
  }
}
