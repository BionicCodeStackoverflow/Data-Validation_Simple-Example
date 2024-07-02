namespace DataValidation.Main
{
  using System;
  using System.Collections.Generic;
  using System.ComponentModel;
  using System.ComponentModel.DataAnnotations;
  using System.Globalization;
  using System.Linq;
  using System.Reflection;
  using System.Runtime.CompilerServices;
  using System.Text;
  using System.Threading.Tasks;

  public class ViewModel : INotifyPropertyChanged, INotifyDataErrorInfo
  {
    // Constructor
    public ViewModel()
    {
      this.errors = new Dictionary<string, IList<object>>();
      this.errorsInternal = new Dictionary<string, IList<object>>();
      this.validationRules = new Dictionary<string, HashSet<IValidationRule>>();
      this.decoratedPropertyInfoMap = new Dictionary<string, PropertyInfo>();

      this.propertyInfoMap = GetType().GetProperties(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public)
        .ToDictionary(propertyInfo => propertyInfo.Name);

      // Collect all properties that are decorated with validation attributes
      this.decoratedPropertyInfoMap = this.propertyInfoMap
        .Where(entry => entry.Value.GetCustomAttributes(typeof(ValidationAttribute)).Any())
        .ToDictionary(entry => entry.Key, entry => entry.Value);

      this.decoratedPropertyInfoMap.Add(ViewModel.AllPropertiesString, null);
    }

    /// <summary>
    /// Register a set of <see cref="ValidationRule{TValue}"/> with a property. 
    /// <br/>Registered validation rules will be invoked automatically on property changed.
    /// </summary>
    /// <param name="propertyName">The name of the property that the registered <see cref="ValidationRule{TValue}"/> is associated with.
    /// <br/>If the value is <see langword="null"/> or an empty <see cref="string"/>, the rule will be applied to all properties when <see cref="OnPropertyChanged(string)"/> is called by passing in <see langword="null"/> or an empty <see cref="string"/> as an argument.</param>
    /// <param name="validationRules">A set of rules that are applied to the property specified by the <paramref name="propertyName"/>. 
    /// <br/>Inherit from the abstract <see cref="ValidationRule{TValue}"/> to create a validation rule 
    /// <br/> and return <see cref="ValidationResult.ValidResult"/> if the validation passed, or create a custom instance that contains the error message or messages for the property.</param>
    protected void RegisterPropertyValidation(string propertyName, IEnumerable<IValidationRule> validationRules)
    {
      foreach (IValidationRule validationRule in validationRules)
      {
        RegisterPropertyValidation(propertyName, validationRule);
      }
    }

    /// <summary>
    /// Register a <see cref="ValidationRule{TValue}"/> with a property. 
    /// <br/>Registered validation rules will be invoked automatically on property changed.
    /// </summary>
    /// <param name="propertyName">The name of the property that the registered <see cref="ValidationRule{TValue}"/> is associated with.
    /// <br/>If the value is <see langword="null"/> or an empty <see cref="string"/>, the rule will be applied to all properties when <see cref="OnPropertyChanged(string)"/> is called by passing in <see langword="null"/> or an empty <see cref="string"/> as an argument.</param>
    /// <param name="validationRule">A rule that is applied to the property specified by the <paramref name="propertyName"/>. 
    /// <br/>Inherit from the abstract <see cref="ValidationRule{TValue}"/> to create a validation rule 
    /// <br/> and return <see cref="ValidationResult.ValidResult"/> if the validation passed, or create a custom instance that contains the error message or messages for the property.</param>
    protected void RegisterPropertyValidation(string propertyName, IValidationRule validationRule)
    {
      if (string.IsNullOrEmpty(propertyName))
      {
        propertyName = ViewModel.AllPropertiesString;
      }

      if (!this.validationRules.TryGetValue(propertyName, out HashSet<IValidationRule> validationRulesForProperty))
      {
        validationRulesForProperty = new HashSet<IValidationRule>();
        this.validationRules.Add(propertyName, validationRulesForProperty);
      }

      validationRulesForProperty.Add(validationRule);
    }

    /// <summary>
    /// Register a delegate with a property. 
    /// <br/>Registered validation delegates will be invoked automatically on property changed.
    /// </summary>
    /// <param name="propertyName">The name of the property that the registered <see cref="ValidationRule{TValue}"/> is associated with.
    /// <br/>If the value is <see langword="null"/> or an empty <see cref="string"/>, the rule will be applied to all properties when <see cref="OnPropertyChanged(string)"/> is called by passing in <see langword="null"/> or an empty <see cref="string"/> as an argument.</param>
    /// <param name="validationDelegate">A delegate that is applied to the property specified by the <paramref name="propertyName"/>. 
    /// <br/>Inherit from the abstract <see cref="ValidationRule{TValue}"/> to create a validation rule 
    /// <br/> and return <see cref="ValidationResult.ValidResult"/> if the validation passed, or create a custom instance that contains the error message or messages for the property.</param>
    protected void RegisterPropertyValidation<TValue>(string propertyName, Func<TValue, ValidationResult> validationDelegate)
    {
      if (validationDelegate is null)
      {
        throw new ArgumentNullException(nameof(validationDelegate));
      }

      var validationRule = new ValidationRuleInternal<TValue>(validationDelegate);
      RegisterPropertyValidation(propertyName, validationRule);
    }

    /// <summary>
    /// Register a set of delegates with a property. 
    /// <br/>Registered validation delegates will be invoked automatically on property changed.
    /// </summary>
    /// <param name="propertyName">The name of the property that the registered <see cref="ValidationRule{TValue}"/> is associated with.
    /// <br/>If the value is <see langword="null"/> or an empty <see cref="string"/>, the rule will be applied to all properties when <see cref="OnPropertyChanged(string)"/> is called by passing in <see langword="null"/> or an empty <see cref="string"/> as an argument.</param>
    /// <param name="validationDelegates">A set of delegates that are applied to the property specified by the <paramref name="propertyName"/>. 
    /// <br/>The delegate should return <see cref="ValidationResult.ValidResult"/> if the validation passed, 
    /// <br/>or create a custom instance that contains the error message or messages for the property.</param>
    protected void RegisterPropertyValidation<TValue>(string propertyName, IEnumerable<Func<TValue, ValidationResult>> validationDelegates)
    {
      if (validationDelegates is null)
      {
        throw new ArgumentNullException(nameof(validationDelegates));
      }

      foreach (Func<TValue, ValidationResult> validationDelegate in validationDelegates)
      {
        RegisterPropertyValidation(propertyName, validationDelegate);
      }
    }

    // Example uses System.ValueTuple.
    // Method is supposed to be called from each property which needs to validate its value.
    // Because the parameter 'propertyName' is decorated with the 'CallerMemberName' attribute.
    // this parameter is automatically generated by the compiler. 
    protected bool IsPropertyValid<TValue>(
      TValue value,
      Func<TValue, ValidationResult> validationDelegate,
      [CallerMemberName] string propertyName = null) 
      => ValidateWithDelegate(value, validationDelegate, propertyName, isInternalValidation: false);

    // Validation method. 
    // Is invoked by 'OnPropertyChanged' (see below).
    // Uses ValidationRule to validate the property value.
    // validationRules must be registered by adding them to the validationRules dictionary.
    //
    // Because the parameter 'propertyName' is decorated with the 'CallerMemberName' attribute.
    // this parameter is automatically generated by the compiler. 
    // The caller only needs to pass in the 'propertyValue', if the caller is the target property's set method.
    protected bool IsPropertyValid<TValue>(TValue propertyValue, [CallerMemberName] string propertyName = null)
      => IsPropertyValidInternal(propertyValue, propertyName, isInternalValidation: false);

    protected bool IsPropertyValid<TValue>([CallerMemberName] string propertyName = null) 
      => IsPropertyValidInternal(propertyName, isInternalValidation: false);

    // Validate property using decorating attributes. 
    // Is invoked by 'OnPropertyChanged' (see below).
    protected bool IsAttributedPropertyValid([CallerMemberName] string propertyName = null)
    {
      if (string.IsNullOrWhiteSpace(propertyName))
      {
        bool hasInvalidValue = false;
        foreach (KeyValuePair<string, PropertyInfo> entry in this.decoratedPropertyInfoMap)
        {
          hasInvalidValue |= !ValidateWithAttributes(entry.Key, isInternalValidation: false);
        }

        return hasInvalidValue;
      }

      return ValidateWithAttributes(propertyName, isInternalValidation: false);
    }

    private bool IsPropertyValidInternal(object propertyValue, string propertyName, bool isInternalValidation)
    {
      // If propertyName is null or empty then validate the complete view model
      if (string.IsNullOrWhiteSpace(propertyName))
      {
        bool hasValidationErrors = false;
        foreach (KeyValuePair<string, PropertyInfo> entry in this.propertyInfoMap)
        {
          hasValidationErrors |= !IsPropertyValid(entry.Key);
        }

        return hasValidationErrors;
      }

      return this.propertyInfoMap.ContainsKey(propertyName)
        ? ValidateWithValidationRules(propertyValue, propertyName, isInternalValidation)
        : throw new ArgumentException("Invalid property name.", nameof(propertyName));
    }

    private bool IsPropertyValidInternal(string propertyName, bool isInternalValidation)
    {
      if (this.propertyInfoMap.TryGetValue(propertyName, out PropertyInfo propertyInfo))
      {
        object propertyValue = propertyInfo.GetValue(this);
        return IsPropertyValidInternal(propertyValue, propertyName, isInternalValidation);
      }

      throw new ArgumentException("Invalid property name.", nameof(propertyName));
    }

    private bool IsAttributedPropertyValidInternal(string propertyName, bool isInternalValidation)
    {
      if (string.IsNullOrWhiteSpace(propertyName))
      {
        bool hasInvalidValue = false;
        foreach (KeyValuePair<string, PropertyInfo> entry in this.decoratedPropertyInfoMap)
        {
          hasInvalidValue |= !ValidateWithAttributes(entry.Key, isInternalValidation);
        }

        return hasInvalidValue;
      }

      return ValidateWithAttributes(propertyName, isInternalValidation);
    }

    private bool ValidateWithDelegate<TValue>(TValue value, Func<TValue, ValidationResult> validationDelegate, string propertyName, bool isInternalValidation)
    {
      // Clear previous errors of the current property to be validated 
      if (!isInternalValidation)
      {
        _ = ClearErrors(propertyName);
      }

      // Validate using the delegate
      ValidationResult validationResult = validationDelegate?.Invoke(value) ?? ValidationResult.ValidResult;

      if (!validationResult.IsValid)
      {
        // Store the error messages of the failed validation
        AddErrorRange(propertyName, validationResult.ErrorMessages, isInternalValidation, isWarning: false);
      }

      return validationResult.IsValid;
    }

    private bool ValidateWithValidationRules(object propertyValue, string propertyName, bool isInternalValidation)
    {
      // Clear previous errors of the current property to be validated 
      if (!isInternalValidation)
      {
        _ = ClearErrors(propertyName);
      }

      if (this.validationRules.TryGetValue(propertyName, out HashSet<IValidationRule> propertyValidationRules))
      {
        // Apply all the rules that are associated with the current property 
        // and validate the property's value            
        IEnumerable<object> errorMessages = propertyValidationRules
          .Select(validationRule => validationRule.Validate(propertyValue, CultureInfo.CurrentCulture))
          .Where(result => !result.IsValid)
          .SelectMany(invalidResult => invalidResult.ErrorMessages);
        AddErrorRange(propertyName, errorMessages, isInternalValidation, isWarning: false);

        return !errorMessages.Any();
      }

      // No rules found for the current property
      return true;
    }

    private bool ValidateWithAttributes(string propertyName, bool isInternalValidation)
    { 
      // Clear previous errors of the current property to be validated 
      if (!isInternalValidation)
      {
        _ = ClearErrors(propertyName);
      }

      // The result flag
      bool isValueValid = true;

      if (!this.decoratedPropertyInfoMap.TryGetValue(propertyName, out PropertyInfo propertyInfo))
      {
        throw new ArgumentException("Invalid property name.", nameof(propertyName));
      }

      object propertyValue = propertyInfo.GetValue(this);
      var validationContext = new ValidationContext(this, null, null) { MemberName = propertyName };
      var validationResults = new List<System.ComponentModel.DataAnnotations.ValidationResult>();
      if (!Validator.TryValidateProperty(propertyValue, validationContext, validationResults))
      {
        isValueValid = false;
        AddErrorRange(propertyName, validationResults.Select(attributeValidationResult => attributeValidationResult.ErrorMessage), isInternalValidation, isWarning: false);
      }

      return isValueValid;
    }

    // Adds the specified errors to the errors collection if it is not 
    // already present, inserting it in the first position if 'isWarning' is 
    // false. Raises the ErrorsChanged event if the errors collection changes. 
    // A property can have multiple errors.
    private void AddErrorRange(string propertyName, IEnumerable<object> newErrors, bool isInternalValidation, bool isWarning = false)
    {
      if (!newErrors.Any())
      {
        return;
      }

      IList<object> propertyErrors;
      if (isInternalValidation)
      {
        if (!this.errorsInternal.TryGetValue(propertyName, out propertyErrors))
        {
          propertyErrors = new List<object>();
          this.errorsInternal.Add(propertyName, propertyErrors);
        }
      }
      else
      {
        if (!this.errors.TryGetValue(propertyName, out propertyErrors))
        {
          propertyErrors = new List<object>();
          this.errors.Add(propertyName, propertyErrors);
        }
      }            

      if (isWarning)
      {
        foreach (object error in newErrors)
        {
          propertyErrors.Add(error);
        }
      }
      else
      {
        foreach (object error in newErrors)
        {
          propertyErrors.Insert(0, error);
        }
      }

      OnErrorsChanged(propertyName);
    }

    // Removes all errors of the specified property. 
    // Raises the ErrorsChanged event if the errors collection changes. 
    protected bool ClearErrors(string propertyName)
      => ClearErrorsInternal(propertyName, false);

    // Removes all errors of all properties. 
    // Raises the ErrorsChanged event if the errors collection changes. 
    protected bool ClearAllErrors()
      => ClearAllErrorsInternal(false);

    private bool ClearErrorsInternal(string propertyName, bool isInternalValidation)
    {
      bool hasErrorRemoved;
      hasErrorRemoved = this.errorsInternal.Remove(propertyName);
      if (!isInternalValidation)
      {
        hasErrorRemoved |= this.errors.Remove(propertyName);
      }

      if (hasErrorRemoved)
      {
        OnErrorsChanged(propertyName);
      }

      return hasErrorRemoved;
    }

    private bool ClearAllErrorsInternal(bool isInternalValidation)
    {
      bool hasClearedErrors = false;
      foreach (KeyValuePair<string, PropertyInfo> entry in this.propertyInfoMap)
      {
        PropertyInfo propertyInfo = entry.Value;
        string propertyName = propertyInfo.Name;
        hasClearedErrors |= ClearErrorsInternal(propertyName, isInternalValidation);
      }

      return hasClearedErrors;
    }

    // Optional method to check if a particular property has validation errors
    public bool PropertyHasErrors(string propertyName) => this.errors.TryGetValue(propertyName, out IList<object> propertyErrors) && propertyErrors.Any();

    #region INotifyDataErrorInfo implementation

    // The WPF binding engine will listen to this event
    public event EventHandler<DataErrorsChangedEventArgs> ErrorsChanged;

    // This implementation of GetErrors returns all errors of the specified property. 
    // If the argument is 'null' instead of the property's name, 
    // then the method will return all errors of all properties.
    // This method is called by the WPF binding engine when ErrorsChanged event was raised and HasErrors return true
    public System.Collections.IEnumerable GetErrors(string propertyName)
    {
      IEnumerable<object> propertyErrors;
      if (string.IsNullOrWhiteSpace(propertyName))
      {
         propertyErrors = this.errors
          .Concat(this.errorsInternal)
          .SelectMany(entry => entry.Value);
      }
      else
      {
        var errorMap = this.errors
          .Concat(this.errorsInternal)
          .ToDictionary(entry => entry.Key, entry => entry.Value);
        propertyErrors = errorMap.TryGetValue(propertyName, out IList<object> errors)
          ? errors 
          : new List<object>();
      }

      return propertyErrors;
    }

    // Returns 'true' if the view model has any invalid property
    public bool HasErrors => this.errors.Any() || this.errorsInternal.Any();

    #endregion

    #region INotifyPropertyChanged implementation

    public event PropertyChangedEventHandler PropertyChanged;

    #endregion

    /// <summary>
    /// Raises the <see cref="INotifyPropertyChanged.PropertyChanged"/> event.
    /// </summary>
    /// <param name="propertyName">The name of the property to raise the <see cref="INotifyPropertyChanged.PropertyChanged"/> event for.
    /// <br/>If the value is <see langword="null"/> or an empty <see cref="string"/> the event is raised for all public properties.</param>
    protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
      if (string.IsNullOrWhiteSpace(propertyName))
      {
        _ = ClearAllErrorsInternal(true);
      }
      else
      {
        _ = ClearErrorsInternal(propertyName, true);
      }

      if (this.decoratedPropertyInfoMap.ContainsKey(propertyName))
      {
        _ = IsAttributedPropertyValidInternal(propertyName, isInternalValidation: true);
      }

      _ = IsPropertyValidInternal(propertyName, isInternalValidation: true);
      this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    protected virtual void OnErrorsChanged(string propertyName) 
      => this.ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(propertyName));

    // Maps a property name to a list of errors that belong to this property
    private readonly Dictionary<string, IList<object>> errors;
    private readonly Dictionary<string, IList<object>> errorsInternal;

    // Maps a property name to a list of validationRules that belong to this property
    private readonly Dictionary<string, HashSet<IValidationRule>> validationRules;

    // List of property names of properties tha are decorated with a ValidationAttribute
    // to improve performance by avoiding unnecessary reflection.
    private readonly Dictionary<string, PropertyInfo> decoratedPropertyInfoMap;
    private readonly Dictionary<string, PropertyInfo> propertyInfoMap;
    private const string AllPropertiesString = "";

    private class ValidationRuleInternal<TValue> : ValidationRule<TValue>
    {
      private readonly Func<TValue, CultureInfo, ValidationResult> validationDelegate;

      public ValidationRuleInternal(Func<TValue, ValidationResult> validationDelegate)
        => this.validationDelegate = (value, culture) => validationDelegate.Invoke(value);

      public ValidationRuleInternal(Func<TValue, CultureInfo, ValidationResult> validationDelegate)
        => this.validationDelegate = validationDelegate;

      public override ValidationResult Validate(TValue value, CultureInfo cultureInfo) 
        => this.validationDelegate.Invoke(value, cultureInfo);
    }
  }
}
