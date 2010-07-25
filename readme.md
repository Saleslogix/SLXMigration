The Migration Tool is a utility that enables you to convert Sage SalesLogix plugins created for the Sage SalesLogix Network Client in Architect into quick forms for use in the Sage SalesLogix Web Client.

*Note*	This version enables migration of forms created in any version of Sage SalesLogix and scripts created in version 6.0 and later.

Overview
========
Sage SalesLogix is a feature-rich product. Consequently, converting Network Client customizations to the Web Client can be time-intensive. The Migration Tool automates many of the conversion tasks, reducing work by creating forms, scripts, navigation, and data relationships for you.

There are several ways you can use Migration Tool to assist you:
•	Use it to make a quick assessment of what customizations will convert.
•	Learn about conversion by choosing something simple to migrate and studying the outcome.
•	Use it for a full migration of your customizations.

Porting a Sage SalesLogix Network Client customization can be divided into several phases:
•	Use the Architect to make recommended simplifications to your forms and create a project.
•	Use the Application Architect to run the Migration Tool on the legacy project. The Migration Tool converts bound ActiveX forms and scripts to SalesLogix metadata including quick forms definitions, logs conversions, and separates strings and business logic into .NET assemblies.
•	If necessary, adjust the legacy forms and rerun the Migration Tool to fine-tune the migration results.
•	Review newly converted items and adjust Web forms and business logic. Features of the Application Architect such as Remove Row/Column and Cut/Copy/Paste on quick forms, and the Data Sources window can be used to refine your Web form layout.

Install
=======
To install the Migration Tool, extract files from the Migration Tool zip file and edit two files.

File Information
------------------
The Migration Tool zip file contains the following files:

Migration\Borland.Vcl.dll  
Migration\Interop.AxSLXCharts.dll  
Migration\Interop.AxSLXControls.dll   
Migration\Interop.AxSLXDialogs.dll   
Migration\Interop.SalesLogix.dll  
Migration\Interop.SLXCharts.dll   
Migration\Interop.SLXControls.dll   
Migration\Interop.SLXDialogs.dll   
Migration\Interop.SLXOptions.dll   
Migration\Interop.StdType.dll   
Migration\Interop.StdVCL.dll   
Migration\Sage.SalesLogix.Migration.dll   
Migration\Sage.SalesLogix.Migration.Forms.dll   
Migration\Sage.SalesLogix.Migration.Module.dll   
Migration\Sage.SalesLogix.Migration.Script.dll   
Sample\Widgets.sxb  
Sage SalesLogix Migration Tool Guide.pdf

### To install the Migration Tool

1.	Ensure that Sage SalesLogix 7.5.x is installed.
2.	Extract the files and folder structure from SLX_v75x_MigrationTool.zip, to ..\Program Files\SalesLogix.
3.	In a text editor, open the file named ..\Program Files\SalesLogix\SageAppArchitect.exe.config.
4.	Locate the line containing <probing privatePath="Platform;SupportFiles"/> and replace it with <probing privatePath="Platform;SupportFiles;Migration"/>
5.	In a text editor, open the file named ..\Program Files\SalesLogix\AppConfig\SalesLogix.xml.
Caution IncorrectinformationinthisfilecanpreventtheApplicationArchitectfromopening.
6.	Add the following new <Include> child to the <Modules> section beneath the existing children:
<Include ModuleName= "Sage.SalesLogix.Migration.Module.MigrationModule,Sage.SalesLogix.Migration.Module"/>
7.	Restart the Application Architect, and then confirm that the toolbar contains the Migration Tool button.