# Playful Sparkle: Replace Accents

**Playful Sparkle: Replace Accents** is your go-to Visual Studio extension for effortlessly removing accents from text. Utilizing Unicode normalization and customizable mappings, this tool ensures clean text processing for code, databases, and more. Quickly replace accented characters across your entire document, selections, or with multi-cursor.

---

## Features

* **Accent Replacement via Unicode Normalization**: The extension employs Unicode Normalization Form D to decompose characters and subsequently removes combining diacritical marks. This method effectively replaces many accented characters (e.g., `á`, `é`, `ö`) with their unaccented equivalents (e.g., `a`, `e`, `o`).

* **Batch Processing**: Perform accent replacement across the entire active text document, within a selected text range, or across multiple selections using Visual Studio's multi-cursor feature.

* **Customizable Character Mapping**: Define specific character replacements using the **Special Character Mappings** setting. These replacements are applied after the standard Unicode normalization and diacritic removal process, allowing for fine-grained control over how certain characters are handled. Ensure your custom mappings target the base characters (those remaining after diacritic removal).

* **Menu Command**: Initiate the accent replacement process using the **Replace Accents** command found under the **Edit** menu in Visual Studio.

---

## Known Issues

* **Non-Latin Script Support**: The extension primarily relies on Unicode Normalization Form D to decompose accented characters into base characters and combining diacritical marks. While this method effectively handles many accented characters in Latin-based scripts, its effectiveness with other script systems (e.g., Cyrillic, Greek, Arabic, CJK) may be limited. Characters in these scripts might not decompose in the same way, and therefore, the accent removal process might not yield the desired results. Users working with non-Latin scripts may need to rely more heavily on custom mappings defined in the **Special Character Mappings** setting, accessible through **Tools** -> **Options** -> **Replace Accents**. Even with custom mappings, comprehensive support for all non-Latin scripts is not guaranteed.

* **Performance with Large Documents**: The extension processes the selected text content in memory after applying Unicode normalization. For very large selections or entire documents, this in-memory processing, along with the subsequent iteration through each character, could lead to increased memory consumption and processing time within Visual Studio. Users might experience a temporary slowdown or unresponsiveness in the Visual Studio editor, especially with files containing a very high character count or when applying the operation to a large selection. The extension processes selections in reverse order to minimize the impact of replacements on subsequent selections.

* **Custom Mapping Considerations**: Custom mappings are defined in the **Special Character Mappings** setting under **Tools** -> **Options** -> **Replace Accents**. These mappings are applied *after* the Unicode normalization and diacritic removal steps. This implies that custom mappings should generally target the base characters (those remaining after the diacritics are removed). Conflicts in custom mappings could arise if multiple mappings target the same base character, leading to the last defined mapping taking precedence. Additionally, if a custom mapping is intended to handle a character that the normalization process already modifies or removes, the custom mapping might not behave as expected. Users should carefully consider the interaction between Unicode normalization and their custom mappings when configuring the **Special Character Mappings**.

If you encounter any of these or other issues, please report them on the [GitHub Issues page](https://github.com/playfulsparkle/vs_ps_replace_accents/issues) with detailed steps to reproduce the problem.

---

## Release Notes

### 0.0.1

* Initial public release of the **Playful Sparkle: Replace Accents** extension for Visual Studio.
* Implemented core text processing functionality leveraging Unicode Normalization Form D for the replacement of accented characters with their unaccented counterparts. This includes handling a wide range of diacritical marks present in Latin-based scripts.
* Introduced support for user-defined custom character mappings via the **Special Character Mappings** setting, accessible through **Tools** -> **Options** -> **Replace Accents**. This feature allows users to specify additional or override default replacement rules for specific characters, providing enhanced flexibility in text normalization.

---

## Support

For any inquiries, bug reports, or feature requests related to the **Playful Sparkle: Replace Accents** extension, please feel free to utilize the following channels:

* **GitHub Issues**: For bug reports, feature suggestions, or technical discussions, please open a new issue on the [GitHub repository](https://github.com/playfulsparkle/vs_ps_replace_accents/issues). This allows for community visibility and tracking of reported issues.

* **Email Support**: For general questions or private inquiries, you can contact the developer directly via email at `support@playfulsparkle.com`. Please allow a reasonable timeframe for a response.

We encourage users to use the GitHub Issues page for bug reports and feature requests as it helps in better organization and tracking of the extension's development.

---

## License

This extension is licensed under the [BSD-3-Clause License](https://github.com/playfulsparkle/vs_ps_replace_accents/blob/main/LICENSE). See the `LICENSE` file for complete details.

---

## Author

Hi! We're the team behind Playful Sparkle, a creative agency from Slovakia. We got started way back in 2004 and have been having fun building digital solutions ever since. Whether it's crafting a brand, designing a website, developing an app, or anything in between, we're all about delivering great results with a smile. We hope you enjoy using our Visual Studio extension!

---