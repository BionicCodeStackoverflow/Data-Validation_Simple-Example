# Data_Validation_Example

[![BC](https://img.shields.io/badge/.NET-informational)](https://github.com/BionicCode/BionicCode.Net#bioniccodenet--)
[![BC](https://img.shields.io/badge/.NET-Framework-informational)](https://github.com/BionicCode/BionicCode.Net#bioniccodenet--)
[![BC](https://img.shields.io/badge/-WPF-informational?logo=windows)](https://github.com/BionicCode/BionicCode.Net#bioniccodenet--)

Example #1 that shows how to implement [`INotifyDataErrorInfo`](https://learn.microsoft.com/en-us/dotnet/api/system.componentmodel.inotifydataerrorinfo?view=net-8.0).  
Example #2 shows how to implement attribute based validation using [`ValidationAttribute`](https://learn.microsoft.com/en-us/dotnet/api/system.componentmodel.dataannotations.validationattribute?view=net-8.0).
Example #3 combines both validation variants.

The example aimed to provide a reusable solution and therefore does not refelect the most simplistic implementation.
The example contains a fully working and reusable base class [`ViewModel`](https://github.com/BionicCodeStackoverflow/Data_Validation_Example/blob/main/DataValidation/DataValidation.Main/ViewModel.cs) for all view models.  
This base class `ViewModel` implements `INotifyDataErrorInfo` and `INotifyPropertyChanged`.  
Simply extend `ViewModel` to get the features:

```c#
public class MainViewModel : ViewModel
{
  // Example #1, which uses implicit validation:
  // the validation method 'IsPropertyValid' is invoked by the event invocator OnPropertyChanged.
  // To validate a value outside the property, simply call 'IsPropertyValid(value)'.
  //
  // Enabled because there exists a ValidationRule (UserInputValidationRule)
  // that was registered with this property by calling 'RegisterPropertyValidation()'.
  private string userInput;
  public string UserInput
  {
    get => this.userInput;
    set
    {
      this.userInput = value;
      OnPropertyChanged();
    }
  }

  // Example #2: validate the value by defining a delegate (or lambda)
  private string userInput1;
  public string UserInput1
  {
    get => this.userInput1;
    set
    {
      // Define the delegat or method group
      Func<string, ValidationResult> validationDelegate = input => input.StartsWith("@") 
        ? ValidationResult.ValidResult 
        : new ValidationResult(false, "Input must start with '@'.");
      bool isValueValid = IsPropertyValid(value, validationDelegate);

      // Optionally reject value if validation has failed
      if (isValueValid)
      {
        this.userInput1= value;
        OnPropertyChanged();
      }
    }
  }

  // Example #3: validate with ValidationAttributes.
  // The validation method 'IsAttributedPropertyValid' is invoked by the event invocator OnPropertyChanged.
  // To validate a value outside the property, simply call 'IsAttributedPropertyValid(value)'.
  private string userInput2;

  [MaxLength(3, ErrorMessage = "Input too long. Input must contain 3 or less characters.")]
  public string UserInput2
  {
    get => this.userInput2;
    set
    {
        
      this.userInput2 = value;
      OnPropertyChanged();
    }
  }

  public MainViewModel()
  {
    var validationRule = new UserInputValidationRule();

    // Use one of the overloads to register validation rules to enable auto-validation:
    // validation rules are automatically applied on property changes. 
    RegisterPropertyValidation(nameof(this.UserInput), validationRule);

    // Alternatively, use one of the overloads to register validation delegates to enable auto-validation:
    // validation rules are automatically applied on property changes. 
    RegisterPropertyValidation(nameof(this.UserInput2), (Func<string, ValidationResult>)(value => validationRule.Validate(value)));
  }

  // Example that shows how to check for validation errors before processing property data.
  private void SaveData()
  {
    if (this.HasErrors)
    {
      // Some properties hold invalid data ==> do nothing until the user has fixed them.
      return;
    }

    // TODO::All data is valid ==> Save data
  }
}
```
