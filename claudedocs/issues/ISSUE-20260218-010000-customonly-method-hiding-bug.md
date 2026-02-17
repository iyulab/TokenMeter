# CostCalculator.CustomOnly() uses method hiding instead of virtual override

## 발견 위치
- 파일: `src/TokenMeter/CostCalculator.cs`
- 라인: 57-65 (CustomOnlyCostCalculator 내부 클래스)

## 현상
`CostCalculator.CustomOnly()` 팩토리 메서드가 `CustomOnlyCostCalculator`를 반환하지만, `GetPricing()`이 `new` 키워드로 method hiding되어 있음. 반환 타입이 `CostCalculator`이므로, 호출자는 항상 base 클래스의 `GetPricing()`을 호출하게 됨.

```csharp
// CustomOnlyCostCalculator 내부
public new ModelPricing? GetPricing(string modelId) // 'new' = method hiding
{
    return _customPricing.TryGetValue(modelId, out var pricing) ? pricing : null;
}
```

`CalculateCost()`도 base 클래스에서 `this.GetPricing()`을 호출하므로, `CustomOnly()` 인스턴스도 built-in pricing을 사용하게 됨. 즉 `CustomOnly()`의 "no built-in pricing" 의도가 실현되지 않음.

## 기대 동작
`CustomOnly()` 인스턴스에서는 built-in pricing 없이 커스텀 등록된 모델만 조회되어야 함.

## 제안
`GetPricing()`을 `virtual`로 변경하여 올바른 다형성 사용:

```csharp
public virtual ModelPricing? GetPricing(string modelId)
{
    if (_customPricing.TryGetValue(modelId, out var customPricing))
        return customPricing;
    return ModelPricingData.FindPricing(modelId);
}

private sealed class CustomOnlyCostCalculator : CostCalculator
{
    public override ModelPricing? GetPricing(string modelId)
    {
        return _customPricing.TryGetValue(modelId, out var pricing) ? pricing : null;
    }
}
```

## 심각도
major — CustomOnly() API가 문서된 의도와 다르게 동작
