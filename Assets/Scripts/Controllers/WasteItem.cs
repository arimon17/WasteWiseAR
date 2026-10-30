using UnityEngine;

public enum TrashCategory
{
    Organik = 0,
    Anorganik = 1,
    B3 = 2
}

[DisallowMultipleComponent]
[RequireComponent(typeof(Collider))]
public class WasteItem : MonoBehaviour
{
    [SerializeField] private string itemName;
    [SerializeField] private TrashCategory correctCategory;
    [SerializeField, TextArea] private string explanation;

    public string ItemName => itemName;
    public TrashCategory CorrectCategory => correctCategory;
    public string Explanation => explanation;

    public void Configure(string newItemName, TrashCategory category, string newExplanation)
    {
        itemName = newItemName;
        correctCategory = category;
        explanation = newExplanation;
        gameObject.name = $"Waste_{newItemName}";
    }
}
