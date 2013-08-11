README
======

CodePortify is a Visual Studio extension that can normalize text files in several
ways. The features are:
- tabify or untabify;
- normalize new lines (CRLF, LF or CR);
- default saving to UTF-8 (without Bom).

It can also normalize new lines on pasted text!

It works on a per-project/extension basis: this does mean that you can configure
the behavior of normalization for each extension of your projects, saving the
configuration in the project files or ".sln" solution file.  Also, you can
configure a "do nothing" bevaviour excluding singles extensions or a whole project.
Excuding a project is a storage clean operation: the excusion is persisted in
the ".user" and ".suo" user files respectively for the the projects and the solutions.
This means that you can safely commit code for shared projects without clutter
if you don't use CodePortify on them.

To not interfer with other related mecanisms or to offer a smoother experience,
CodePortify takes control of the following Visual Studio settings:
- Enviromnent -> Documents -> "Check for consistent line endings on load", setting it to
  False
- Text Editor -> General -> "Auto-detect UTF-8 encoding without signature", setting it
  to True;

CodePortify source code is also a chest of gems in Visual Studio Extensibility. It makes
use of several extensibility mechanisms, trying to get the best from each one:
- it's itself a Visual Studio VSPackage, that is the most powerful mean of extending
  Visual Studio;
- it uses MEF (Managed Extensibility Framework) components extensibility;
- it uses the DTE (Development Tools Environment) automation.

CodePortify is a work based on the FixMixedTabs extension, by Noah Richards.

License is MS-PL
