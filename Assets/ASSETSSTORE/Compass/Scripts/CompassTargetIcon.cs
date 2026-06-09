using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform))]
[RequireComponent(typeof(Image))]
public class CompassTargetIcon : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI targetDistanceText;

    private RectTransform rectTransform;
    private Image image;
    private CompassTarget compassTarget;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        image = GetComponent<Image>();
    }

    public void Setup(CompassTarget compassTarget)
    {
        image.sprite = compassTarget.IconSprite;
        this.compassTarget = compassTarget;
    }

    public void UpdateIcon(float screenWidth, float horizontalFOV, float compassUIHorizontalOffSet, RectTransform iconContainer)
    {
        if (compassTarget.TargetIsInFrontOfCamera() == true)
            PlaceIconWhenTargetIsInFront(screenWidth, horizontalFOV, compassUIHorizontalOffSet, iconContainer);
        else
            PlaceIconWhenTargetIsBehind(iconContainer.rect.width);

        float distanceBetweenPlayerAndTarget = compassTarget.DistanceBetweenPlayerAndTarget();

        bool targetIsWithinRange = TargetIsWithinRange(distanceBetweenPlayerAndTarget, compassTarget.VisibilityRange);

        ToggleIconVisibility(targetIsWithinRange);
        ToggleDistanceTextVisibility(targetIsWithinRange);
        UpdateDistanceTextContent(targetIsWithinRange, distanceBetweenPlayerAndTarget);
    }

    private void PlaceIconWhenTargetIsInFront(float screenWidth, float horizontalFOV, float compassUIHorizontalOffSet, RectTransform iconContainer)
    {
        float pixelPosition_X = ScreenPixelPosition_X(screenWidth, horizontalFOV) + compassUIHorizontalOffSet;
        float iconContainerPixelPosition_X = IconContainerLocalPosition_X(pixelPosition_X, iconContainer);

        if (IconPositionIsOutsideOfContainer(iconContainerPixelPosition_X, iconContainer.rect.width, Side.LEFT))
            rectTransform.anchoredPosition = PixelPosition_AtBoundariesOfContainer(iconContainer.rect.width, Side.LEFT);

        else if (IconPositionIsOutsideOfContainer(iconContainerPixelPosition_X, iconContainer.rect.width, Side.RIGHT))
            rectTransform.anchoredPosition = PixelPosition_AtBoundariesOfContainer(iconContainer.rect.width, Side.RIGHT);

        else
            rectTransform.anchoredPosition = new Vector2(iconContainerPixelPosition_X, 0f);
    }

    private void PlaceIconWhenTargetIsBehind(float iconContainerWidth)
    {
        float signedHorizontalAngleFromCameraToTarget = compassTarget.SignedHorizontalAngleFromCameraToTarget();

        if (signedHorizontalAngleFromCameraToTarget >= 0)
            rectTransform.anchoredPosition = PixelPosition_AtBoundariesOfContainer(iconContainerWidth, Side.RIGHT);

        else
            rectTransform.anchoredPosition = PixelPosition_AtBoundariesOfContainer(iconContainerWidth, Side.LEFT);
    }

    private void ToggleIconVisibility(bool targetIsWithinRange)
    {
        if (targetIsWithinRange == false)
            image.enabled = !compassTarget.HideWhenOutsideOfRange;

        else
            image.enabled = true;
    }

    private void ToggleDistanceTextVisibility(bool targetIsWithinRange)
    {
        if (targetIsWithinRange == false && compassTarget.HideWhenOutsideOfRange == true)
            targetDistanceText.enabled = false;

        else
            targetDistanceText.enabled = compassTarget.ShowDistanceToTarget;
    }

    private void UpdateDistanceTextContent(bool targetIsWithinRange, float distanceBetweenPlayerAndTarget)
    {
        if (compassTarget.ShowDistanceToTarget == false)
            return;

        if (targetIsWithinRange == false && compassTarget.HideWhenOutsideOfRange == true)
            return;

        if (compassTarget.ShowDistanceToTarget == true)
            targetDistanceText.text = Math.Round(distanceBetweenPlayerAndTarget, compassTarget.DistanceTextRoundDecimals).ToString();
    }

    private float ScreenPixelPosition_X(float screenWidth, float horizontalFOV)
    {
        float halfFOV = Mathf.Deg2Rad * (horizontalFOV / 2f);

        float angle = Mathf.Deg2Rad * compassTarget.SignedHorizontalAngleFromCameraToTarget();

        float t = Mathf.Tan(angle) / Mathf.Tan(halfFOV);

        return (t * 0.5f + 0.5f) * screenWidth;
    }

    private float IconContainerLocalPosition_X(float pixelPosition_X, RectTransform iconContainer)
    {
        Vector2 result;

        Vector2 screenPoint = new Vector2(pixelPosition_X, 0f);

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            iconContainer,
            screenPoint,
            null,
            out result
        );

        return result.x;
    }

    private bool IconPositionIsOutsideOfContainer(float containerLocalPosition_X, float iconContainerWidth, Side side)
    {
        return side == Side.LEFT
            ? containerLocalPosition_X < -iconContainerWidth * 0.5f
            : containerLocalPosition_X > iconContainerWidth * 0.5f;
    }

    private Vector2 PixelPosition_AtBoundariesOfContainer(float iconContainerWidth, Side side)
    {
        return side == Side.LEFT
            ? new Vector2(-iconContainerWidth * 0.5f, 0f)
            : new Vector2(iconContainerWidth * 0.5f, 0f);
    }

    private bool TargetIsWithinRange(float distance, float range)
    {
        return distance <= range;
    }

    enum Side
    {
        LEFT,
        RIGHT
    }
}
