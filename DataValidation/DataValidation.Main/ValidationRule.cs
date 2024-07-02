namespace DataValidation.Main
{
  using System;
  using System.Collections.Generic;
  using System.Globalization;
  using System.Linq;
  using System.Text;
  using System.Threading.Tasks;
  using DataValidation.Main.Generic;

  public abstract class ValidationRule<TValue> : IValidationRule, IValidationRule<TValue>
  {
    public CultureInfo Culture { get; set; }

    protected ValidationRule() 
      => this.Culture = CultureInfo.CurrentCulture;

    protected ValidationRule(CultureInfo culture) 
      => this.Culture = culture ?? CultureInfo.CurrentCulture;

    public abstract ValidationResult Validate(TValue value, CultureInfo cultureInfo);

    public ValidationResult Validate(TValue value)
      => Validate(value, this.Culture);

    ValidationResult IValidationRule.Validate(object value, CultureInfo cultureInfo)
      => Validate((TValue)value, cultureInfo);
  }
}
