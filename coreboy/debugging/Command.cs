namespace coreboy.debugging
{
    public interface ICommand
    {
        CommandPattern GetPattern();
        void Run(CommandPattern.ParsedCommandLine commandLine);
    }
}