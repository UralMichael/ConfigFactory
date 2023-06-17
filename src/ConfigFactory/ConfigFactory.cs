﻿using ConfigFactory.Core;
using ConfigFactory.Core.Attributes;
using ConfigFactory.Generics;
using ConfigFactory.Models;
using System.Reflection;

namespace ConfigFactory;

public static class ConfigFactory
{
    private static readonly List<IControlBuilder> _builders = new();
    private static event Action? RegisterDefaults;

    static ConfigFactory() => RegisterDefaults?.Invoke();

    public static ConfigPageModel Build<T>() where T : ConfigModule<T>, new() => Build(ConfigModule<T>.Shared);
    public static ConfigPageModel Build<T>(T module) where T : IConfigModule
    {
        ConfigPageModel configPageModel = new();
        configPageModel.Append(module);
        return configPageModel;
    }

    public static ConfigPageModel Append<T>(this ConfigPageModel configPageModel) where T : ConfigModule<T>, new() => Append(configPageModel, ConfigModule<T>.Shared);
    public static ConfigPageModel Append<T>(this ConfigPageModel configPageModel, T module) where T : IConfigModule
    {
        configPageModel.PrimaryButtonEvent += () => {
            module.Save();
            return Task.CompletedTask;
        };

        foreach ((_, (PropertyInfo info, ConfigAttribute attribute)) in module.Properties) {
            object? value = info.GetValue(module, null);
            if (_builders.FirstOrDefault(x => x.IsValid(value)) is IControlBuilder builder) {
                ConfigGroup group = GetConfigGroup(configPageModel, attribute);
                group.Items.Add(new ConfigItem {
                    Content = builder.Build(module, info),
                    Description = attribute.Description,
                    Header = attribute.Header
                });
            }
        }

        return configPageModel;
    }

    public static void RegisterBuilder<T>() where T : ControlBuilder<T>, new() => Register(ControlBuilder<T>.Shared);
    public static void Register(this IControlBuilder builder)
    {
        _builders.Add(builder);
    }


    private static ConfigGroup GetConfigGroup(ConfigPageModel configPageModel, ConfigAttribute attribute)
    {
        bool isFirstCategory = false;

        ConfigCategory category = configPageModel.Categories.FirstOrDefault(
            x => x.Header == attribute.Category) is ConfigCategory _category
            ? _category : new(parent: configPageModel, attribute.Category, out isFirstCategory);

        ConfigGroup group = category.Groups.FirstOrDefault(
            x => x.Header == attribute.Group) is ConfigGroup _group
            ? _group : new(parent: category, attribute.Group);

        if (isFirstCategory) {
            configPageModel.SelectedGroup = group;
        }

        return group;
    }
}