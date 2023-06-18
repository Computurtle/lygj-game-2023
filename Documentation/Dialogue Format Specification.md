# Dialogue Files

Dialogue files are used to create dialogue chains. They are written in a custom language, which is mainly parsed at compilation-time. They support features such as [labels](#labels), [choices](#choices), and [methods](#method-invocations) for a wide range of extensible use cases. They are also designed to be human-readable, and so are easy to write and understand.

To create a dialogue file, simply create a new file with the `.diag` extension. For example, `MyDialogue.diag`. A custom scripted importer has been provided such that, when dragged into Unity, the file will be compiled into a `DialogueChain` asset. This asset can then be used in-game to play the dialogue chain.

Trivia: Alternatively, use the right-click context menu `Create > LYGJ > Dialogue > Dialogue Chain` to create a new dialogue chain asset in the current folder, and open it in your default text editor. Double-clicking the asset will also open it in your default text editor.

## Syntax

NPCs are defined by a line prefixed with `npc-name:`, where `npc-name` is the key of the NPC. This is used to determine who is speaking, and so must be unique. This leverages the existing `NPCs` class, which if you have not yet used, is a simple class that maps a key to an `NPCBase`. An example of an `NPCBase` is the `NPC` class, which is a simple implementation providing a custom per-NPC key/name, and also the `PlayerNPC` class, which is a special implementation that represents the player. The `NPCs` class is a static helper utility, and so can be used anywhere in your code. For example:

```csharp
if (NPCs.TryGet("npc-name", out var npc)) {
    // ...
}
```

`player` is a special key, which is used to represent the player. This is useful for when you want to have the player speak.

### Dialogue

```diag
npc-name: "Dialogue"
npc-name: "More Dialogue"
player: "Player Dialogue"

# Alternatively
npc-name: "Dialogue" "More Dialogue"
player: "Player Dialogue"
# If the same NPC is saying multiple things in a row, you can use the above syntax to save space. This is especially useful, since each message box should be kept to a single line or two, and the dialogue file would quickly become bloated if you had to write the NPC's name for each line of dialogue.
```

### Labels

```diag
npc-name: "Dialogue"
goto banana

:apple
npc-name: "Unseen Dialogue"
goto end

:banana
npc-name: "Seen Dialogue"
goto end

:end
npc-name: "Dialogue"
```

Note, labels do not have to be defined before they are used, and can be included in any order, due to the semi-compiled nature of DialogueChains. However, it is still recommended to keep them in the order they are used, for readability.

Additionally, labels can be used to create loops, by having a label block jump to itself. For example:

```diag
:loop
npc-name: "Looping..."
goto loop
```

Trivia: `goto` is an alias for `jump`. Saying `goto banana` is equivalent to saying `jump banana`.

### Choices

```diag
> "Choice 1 Text" choice-1-label
> "Choice 2 Text" choice-2-label

:choice-1-label
npc-name: "Choice 1 Dialogue"

:choice-2-label
npc-name: "Choice 2 Dialogue"
```

The choices system is dependent on labels. Each line prefixed with `>` is a choice, and the text following it is the text that will be displayed in the button. The text following without quotes is the label that will be jumped to if the choice is selected.
As you must manually define an 'exit' option, it is safe to assume anything immediately after a block of choices will not be run, for example:

```diag
> "Choice 1 Text" choice-1-label
> "Choice 2 Text" choice-2-label
npc-name: "You'll never see this"

:choice-1-label
# ...
```

Trivia: It is assumed that the player is the one making the choice. As of yet, there is no support for NPCs making choices, as this seems like a very niche use case.

### Method Invocations

#### Direct Invocation

```diag
npc-name: "I'm invoking a method!"
$method-name arg1 arg2 arg3 ...
npc-name: "I'm done invoking a method!"
```

Above is what is reffered to as a 'direct' method invocation. It is a method that is called immediately. The method may return a string, which is the name of a label to jump to. For example:

```diag
$additem item-name 3

:additem-success
npc-name: "You got 3 item-name!"

:additem-failure
npc-name: "You did not have enough inventory space!"
```

#### Indirect Invocation

```diag
npc-name: "You are {random 1 10} years old!"
```

Above is an 'indirect' method invocation. It is a method that is called when the message is displayed. The method may return a string, which is the text to add to the message.

When a method is invoked indirectly, the current speaker can also be used in-scripting. This can be accessed via the `Dialogue.CurrentSpeaker` property.

```diag
npc-name: "I'm {emote grin}happy!"
```

```csharp
class SomeClass {
    [DialogueFunction]
    public static void Emote(Emotion emotion) {
        NPCBase speaker = NPCs.Get(Dialogue.CurrentSpeaker);
        speaker.SetEmotion(emotion);
    }
}
```

The above example is a simple implementation of the `emote` method. It takes a single argument, which is the name of an emotion. It then sets the emotion of the current speaker to that emotion. This is useful for when you want to change the emotion of the speaker mid-dialogue. See [Defining Methods](#defining-methods) for more information on how to define methods.

Note, if you would like to use the `{` or `}` characters in your message, you must escape them with a backslash (`\`). For example:

```diag
npc-name: "This calls a method: {method-name}" # Output: "This calls a method: result"
npc-name: "This does not call a method: \{method-name\}" # Output: "This does not call a method: {method-name}"
```

#### Defining Methods

Methods can be defined in any C# script, so long as the following conditions are met:

1. The method is static, or the defining class inherits from `SingletonMB<T>`. This is because we must know on whom to call the method. If this was defined in some class that is non-static or not a singleton, we would have no way of knowing which instance to call the method on.
2. The method is prefixed with the `[DialogueFunction]` attribute. This is so that the dialogue parser knows which methods are valid to call.

```csharp
class SomeClass {
    [DialogueFunction]
    public static string AddItem(string[] args) {
        // ...
    }

    [DialogueFunction("custom-name")]
    public static string SomeOtherName(string[] args) {
        // ...
    }
}
```

3. The method returns a string, which is the name of a label to jump to (assuming direct invocation) or the text to append (assuming indirect invocation). The method may also return void, in which case no label will be jumped to and no text will be appended.
4. The method either takes no arguments, a single `string[]` argument, or a combination of **primitive only** arguments.
If the method takes a single `string[]` argument, the engine will pass the arguments to the method as a string array, where each element is a single word. For example, if the method is called as `$method-name arg1 arg2 arg3`, the `string[]` argument will be `["arg1", "arg2", "arg3"]`. This also handles quoted arguments, so `$method-name "arg 1" "arg 2"` will result in `["arg 1", "arg 2"]`. You can escape quotes to include them in the string (`\"`).
If the method takes any amount of primitive arguments (`int`, `float`, `bool`, etc.), they will be parsed and handed over. Whilst useful, certain types cannot be handled (for example, collections), and so the `string[]` argument is recommended for more complex methods.

In summary, any of the below method signatures are valid:

```csharp
class SomeClass {
    [DialogueFunction] public static void MethodName1() { }
    [DialogueFunction] public static string MethodName2() { }
    [DialogueFunction] public static void MethodName3(string[] Args) { }
    [DialogueFunction] public static string MethodName4(string[] Args) { }
    [DialogueFunction] public static void MethodName5(int Arg1, float Arg2, bool Arg3) { }
    [DialogueFunction] public static string MethodName6(int Arg1, float Arg2, bool Arg3) { }
    [DialogueFunction] static void MethodName7() { }
    [DialogueFunction] static string MethodName8() { }
    [DialogueFunction] static void MethodName9(string[] Args) { }
    [DialogueFunction] static string MethodName10(string[] Args) { }
    [DialogueFunction] static void MethodName11(int Arg1, float Arg2, bool Arg3) { }
    [DialogueFunction] static string MethodName12(int Arg1, float Arg2, bool Arg3) { }
}

class SomeInstancedClass : SingletonMB<SomeInstancedClass> {
    [DialogueFunction] public void MethodName13() { }
    [DialogueFunction] public string MethodName14() { }
    [DialogueFunction] public void MethodName15(string[] Args) { }
    [DialogueFunction] public string MethodName16(string[] Args) { }
    [DialogueFunction] public void MethodName17(int Arg1, float Arg2, bool Arg3) { }
    [DialogueFunction] public string MethodName18(int Arg1, float Arg2, bool Arg3) { }
    [DialogueFunction] void MethodName19() { }
    [DialogueFunction] string MethodName20() { }
    [DialogueFunction] void MethodName21(string[] Args) { }
    [DialogueFunction] string MethodName22(string[] Args) { }
    [DialogueFunction] void MethodName23(int Arg1, float Arg2, bool Arg3) { }
    [DialogueFunction] string MethodName24(int Arg1, float Arg2, bool Arg3) { }
}
```

### Exiting

```diag
npc-name: "Will you help me?"
> "Yes" choice-yes
> "No" choice-no
> "Maybe" choice-maybe
> "Ask again later" choice-later

:choice-yes
npc-name: "Thank you!"
exit 0

:choice-no
npc-name: "Oh..."
exit 1

:choice-maybe
npc-name: "Okay..."
exit -1

:choice-later
npc-name: "Okay, I'll ask again later."
```

The `exit` command is used to exit the dialogue. It takes a single argument, which is the exit code. This is useful for determining what the player chose, and can be used to determine what to do next in-scripting. When a class calls `DialogueChain.Play()`, it will return the exit code of the dialogue.

The exit code is optional, and if not provided, will default to `0`.

Finally, on the last line of a dialogue file, the `exit` command is implied, and so does not need to be written. For example:

```diag
npc-name: "I say something"
npc-name: "I say something else"
```

Is equivalent to:

```diag
npc-name: "I say something"
npc-name: "I say something else"
exit 0
```

### Comments

```diag
# This is a comment
npc-name: "This is not a comment" # But this is
```

Comments are prefixed with `#`, and can be placed anywhere in the file. They are ignored by the parser, and so can be used to add notes to the file.

### Variables

Variables were a planned feature, and whilst implemented in-scripting, are not yet implemented in the dialogue files. They will be added in a future update given enough demand.
To utilise variables in-scripting, you can use the `Dialogue.Set<T>(string Name, T Value)` method to set a variable, and the `Dialogue.Get<T>(string Name)` method to get a variable. For example:

```csharp
Dialogue.Set("variable-name", 5);
Dialogue.Get<int>("variable-name"); // 5
```

Variables have 'scopes', which are useful for nested calls. For example:

```csharp
Dialogue.Set("variable-name", 5);
Dialogue.Push();
Dialogue.Set("variable-name", 10);
Dialogue.Get<int>("variable-name"); // 10
Dialogue.Pop();
Dialogue.Get<int>("variable-name"); // 5
```

Since the main functionality of dialogue variables exist, you can theoretically implement them yourself in-scripting. For example:

```diag
$set variable-name 5
npc-name: "I'm {get variable-name} years old!"
```

```csharp
public static class DialogueVariableFunctions {
    [DialogueFunction]
    public static void Set(string[] args) {
        Dialogue.Set(args[0], args[1]);
    }

    [DialogueFunction]
    public static string Get(string[] args) {
        return Dialogue.Get<object>(args[0]).ToString();
    }
}
```
