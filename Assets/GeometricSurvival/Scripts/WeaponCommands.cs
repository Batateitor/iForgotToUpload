using System.Collections.Generic;

namespace GeometricSurvival
{
    public interface IGameCommand
    {
        void Execute();
        void Undo();
    }
    // Command: input requests are objects, allowing history and undo.
    public sealed class SelectWeaponCommand : IGameCommand
    {
        private readonly WeaponController receiver;
        private readonly int index;
        private WeaponMemento before;
        public SelectWeaponCommand(WeaponController receiver, int index)
        { this.receiver = receiver; this.index = index; }
        public void Execute()
        {
            before = receiver.CaptureMemento();
            receiver.ApplySelection(index);
        }
        public void Undo() { if (before != null) receiver.RestoreMemento(before); }
    }
    public sealed class CommandHistory
    {
        private readonly Stack<IGameCommand> executed = new Stack<IGameCommand>();
        public void Execute(IGameCommand command) { command.Execute(); executed.Push(command); }
        public void UndoLast() { if (executed.Count > 0) executed.Pop().Undo(); }
    }
}
