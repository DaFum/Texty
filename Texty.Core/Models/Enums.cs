namespace Texty.Core.Models;

public enum SnippetHighlightMode
{
    None,
    Highlighted,
    Hidden
}

public enum TriggerType
{
    Autotext,
    Hotkey,
    Regex,
    Clipboard
}

public enum TriggerScope
{
    Any,
    TextFile,
    Email
}

public enum FormFieldType
{
    Text,
    Dropdown,
    Checkbox,
    Radio,
    Slider,
    DatePicker,
    Table
}

public enum RoleName
{
    Owner,
    Editor,
    Reader
}
