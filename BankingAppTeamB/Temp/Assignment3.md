• Make any architectural changes you deem relevant. Fix what is not working, it will make
later assignments more manageable. Feel free to use design patterns if you want.
• Refactor the code to make it more readable: all functions and variables must have a
semantic meaning whenever possible, there may be no abbreviations (yes, not even
lambdas). Deal with magic numbers.
• Make sure there is no business logic in GUI. If you find some then move it to a non-GUI
class.
• No layer shall know directly about the next one, use interfaces to separate them.
• Write unit tests in the manner presented at the course. You should have 100% coverage
on the parts that you should, and none elsewhere. Add integration tests wherever
appropriate. You may use isolation frameworks. Your automated test must have the
same quality and uniformity as the rest of the codebase.
• Use styleCop static code analysis to find issues in the code and fix them, this will also
help to homogenize the various projects a bit so merging them will be less painful.
1. Copy the SE.ruleset file from teams to your project(s) root.
2. Right click on project(s)-> manage NugetPackages
3. On the Browse tab search for -> StyleCop.Analyzers, then install
4. Right click on your project -> Edit project file
5. Search for the following property group and add the line with
"CodeAnalysisRuleSet", if it does not exist add all three.
<PropertyGroup Condition="'$(Configuration)|$(Platform)'=='Debug|AnyCPU'">
<CodeAnalysisRuleSet>SE.ruleset</CodeAnalysisRuleSet>
</PropertyGroup>
6. Build/attempt to run
Ongoing requirements:
• The team lead will present a report before demoing their work on the contributions of
each team member, ideally task/git/chat/sent files history etc. With the scope of proving
that each team member did some non-negligible amount of work.
• SQL queries must still be absent of business logic.
Optional:
• Try to start early and target getting everything done a couple of days before the deadline.
• Use tasks for work tracking and management.