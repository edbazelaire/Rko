using AI;
using Game.AI.BehaviorTrees;
using Tools;

public class CharacterBT : BehaviorTree
{
    protected override Node SetupTree()
    {
        if (m_Controller == null)
        {
            ErrorHandler.Error("No controller found for this CharacterBT");
            return new Node();
        }

        return BTLoader.LoadTree(m_Controller, m_Controller.Character);
    }
}
