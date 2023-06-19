namespace Template.MobileServer.Web.Infrastructure.Validation;

using System.ComponentModel;
using System.Reflection;

using FluentValidation;
using FluentValidation.Resources;

public static class ValidatorConfigurationExtensions
{
    public static ValidatorConfiguration UseDisplayName(this ValidatorConfiguration config)
    {
        config.DisplayNameResolver = (_, memberInfo, _) =>
        {
            var displayName = memberInfo.GetCustomAttribute<DisplayNameAttribute>()?.DisplayName;
            return displayName ?? memberInfo.Name;
        };
        return config;
    }

    public static ValidatorConfiguration UseCustomLocalizeMessage(this ValidatorConfiguration config)
    {
        var languageManager = (LanguageManager)config.LanguageManager;
        languageManager.AddTranslation("ja-JP", "NotNullValidator", "{PropertyName}は必須です");
        languageManager.AddTranslation("ja-JP", "NotEmptyValidator", "{PropertyName}を入力してください");
        languageManager.AddTranslation("ja-JP", "MaximumLengthValidator", "{PropertyName}は{MaxLength}文字以内で入力して下さい");
        languageManager.AddTranslation("ja-JP", "RegularExpressionValidator", "{PropertyName}は不正な値です");
        languageManager.AddTranslation("ja-JP", "GreaterThanOrEqualValidator", "{PropertyName}は{ComparisonValue}以上の値を入力してください");
        languageManager.AddTranslation("ja-JP", "GreaterThanValidator", "{PropertyName}は{ComparisonValue}より大きい値を入力してください");
        languageManager.AddTranslation("ja-JP", "LessThanOrEqualValidator", "{PropertyName}は{ComparisonValue}以下の値を入力してください");
        languageManager.AddTranslation("ja-JP", "LessThanValidator", "{PropertyName}は{ComparisonValue}より小さい値を入力してください");

        return config;
    }
}
