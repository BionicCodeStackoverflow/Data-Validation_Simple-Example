namespace DataValidation.Main
{
  using System;
  using System.Collections.Generic;
  using System.ComponentModel.DataAnnotations;
  using System.Linq;
  using System.Text;
  using System.Threading.Tasks;

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
}
