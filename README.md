# RainForms
A plugin for Rainmeter that lets you use System.Windows.Forms dialogs within Rainmeter

## General
To create a Form with RainForms you have to define a Measure of type "Form" first. You can look up which Properties you can set on the form [here](https://docs.microsoft.com/de-de/dotnet/api/system.windows.forms.form?view=netframework-2.0). You can set any property mentioned at the page. Here is an example:

    [FormTest]           <-- name the measure like you want
    Measure=Plugin       
    Plugin=RainForms     <-- specify RainForms as the Plugin
    Type=Form            <-- specify the type of the control. In this case we need a Form
    Text=TestForm        <-- you can specify som optional propertys if you want like the text of the form or the size
    ClientSize=420,420

After you have created a Form measure, you have to give ithe command to show the form through a bang. I will use the OnRefreshAction to show the form each time we load the skin.

    [Rainmeter]
    OnRefreshAction=[!CommandMeasure FormTest Show]    <-- bang to show the form
    
    [MeterDummy]    <-- the skin wont load if there are no meters, so we make a dummy meter
    Meter=Image
    
If you run the skin you will notice that there will popup an empty Form:
![Empty form](https://i.imgur.com/jBbxluS.png)
But an empty form is not very useful. Lets add some label and button on there.

    [SomeLabel]
    Measure=Plugin
    Plugin=RainForms
    ParentName=FormTest      <-- specify the form measure on which the labe should appear
    Type=Label               <-- specify the type of the control. In this case we need a label
    Text=I'm a label. fml    <-- add some text to the label
    Location=10,10           <-- the location on the form: x and y coordinates
    AutoSize=1               <-- let the label size itself
    Font=Arial,20            <-- lets increase the font size a bit
    
    [ButtonClose]
    Measure=Plugin
    Plugin=RainForms
    ParentName=FormTest
    Type=Button
    Text=Okay... bye
    Font=Arial,15
    Location=110,150
    ClientSize=200,100                           <-- size of the button (width, height)
    OnClick=[!CommandMeasure FormTest Close]     <-- if the button is clicked, close the form
    
![Form with button and label](https://i.imgur.com/wNwaLoO.png)
And if we click the button, the form will close as expected. You can use many more controls than just a label for example a textbox, groupbox, checkbox, tabcontrol etc. , you can use any control in [System.Windows.Forms](https://docs.microsoft.com/de-de/dotnet/api/system.windows.forms?view=netframework-2.0).

## Calling Methods
You can call any method through rainmeter bangs that is avaliable on the specific control. You can look up on msdn which methods are available.
Here are some examples:

    [!CommandMeasure FormTest Show]
    [!CommandMeasure FormTest Close]
    [!CommandMeasure FormTest BringToFront]
    [!CommandMeasure SomeTexbox Focus]
    [!CommandMeasure TreeView Collapse]

## Enums
You can set enums by writing the enum as a string. use "|" to seperate multiple enums

    Anchor=Bottom|Left     <-- set the anchor point to the bottom left
    
## RFTypeInfo and RFPropertyInfo
You can use a bang like this to optain information for a specific type or property:

    [!CommandMeasure FormTest "RFTypeInfo System.Drawing.Size"]
    [!CommandMeasure FormTest "RFPropertyInfo Width"]

Thats it for now, i will add more documentation soon!
