using UnityEngine;
using UnityEngine.EventSystems;

public class MonopolyTileClickHandler : MonoBehaviour, IPointerClickHandler
{
    private MonopolyTileData tileData;

    private void Start()
    {
        tileData = GetComponent<MonopolyTileData>();
        if (tileData == null)
        {
            Debug.LogWarning($"MonopolyTileClickHandler on {gameObject.name} has no MonopolyTileData component!");
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (tileData == null || tileData.spaceData == null) return;

        var space = tileData.spaceData;
        int spaceId = tileData.spaceIndex;
        
        Debug.Log($"??? Clicked on tile: {space.spaceName} (ID: {spaceId})");
        
        // Show property info for all property types
        if (space.type == PropertyType.Property || 
            space.type == PropertyType.Railroad || 
            space.type == PropertyType.Utility)
        {
            bool canPurchase = false;
            
            // Check if the current player can purchase this property
            if (MonopolyGameManager.Instance != null && MonopolyGameManager.Instance.IsMyTurn())
            {
                int myPlayerId = MonopolyGameManager.Instance.GetMyPlayerId();
                var myPlayerData = MonopolyGameManager.Instance.GetPlayer(myPlayerId);
                
                // Check if player is on this space and can afford it
                if (myPlayerData.position == spaceId && 
                    myPlayerData.money >= space.price && 
                    !space.isOwned)
                {
                    canPurchase = true;
                }
            }
            
            // Show property info with the property ID so houses/hotels can be placed correctly
            if (MonopolyUI.Instance != null)
            {
                MonopolyUI.Instance.ShowPropertyInfo(space, spaceId, canPurchase);
            }
        }
        else
        {
            // For special spaces, just show a message
            string message = GetSpaceDescription(space);
            if (MonopolyUI.Instance != null && !string.IsNullOrEmpty(message))
            {
                MonopolyUI.Instance.OnGameMessage($"Clicked on {space.spaceName}: {message}");
            }
        }
    }

    private string GetSpaceDescription(MonopolySpace space)
    {
        switch (space.type)
        {
            case PropertyType.Go:
                return "Collect $200 when you pass or land on GO";
            case PropertyType.Jail:
                return "Just Visiting or In Jail";
            case PropertyType.FreeParking:
                return "Free Parking - Nothing happens";
            case PropertyType.GoToJail:
                return "Go directly to Jail, do not pass GO";
            case PropertyType.Tax:
                return space.spaceName.Contains("Income") ? "Pay $200 Income Tax" : "Pay $75 Luxury Tax";
            case PropertyType.Chance:
                return "Draw a Chance card";
            case PropertyType.CommunityChest:
                return "Draw a Community Chest card";
            default:
                return "";
        }
    }
}