# LLM Pricing Update Guide

이 문서는 TokenMeter의 모델 가격 정보를 주기적으로 업데이트하기 위한 가이드입니다.

## 업데이트 주기

- **권장 주기**: 월 1회 또는 주요 모델 출시 시
- **긴급 업데이트**: 가격 정책 변경 발표 시 즉시

## 공식 가격 정보 소스

### OpenAI
- **URL**: https://openai.com/api/pricing/
- **API 문서**: https://platform.openai.com/docs/pricing
- **주요 모델**: GPT-4o, GPT-4o-mini, o1, o3 시리즈
- **특이사항**:
  - o-series 모델은 "reasoning tokens"을 output으로 청구 (숨겨진 비용 주의)
  - Batch API 50% 할인, Prompt Caching 최대 90% 할인

### Anthropic (Claude)
- **URL**: https://docs.anthropic.com/en/docs/about-claude/pricing
- **API 콘솔**: https://console.anthropic.com/
- **주요 모델**: Claude 4.5 시리즈, Claude 3.5 시리즈
- **특이사항**:
  - Prompt caching: 읽기 0.1x, 쓰기 1.25x~2x
  - Extended thinking은 output 토큰으로 청구

### Google (Gemini)
- **URL**: https://ai.google.dev/gemini-api/docs/pricing
- **Vertex AI**: https://cloud.google.com/vertex-ai/generative-ai/pricing
- **주요 모델**: Gemini 2.5 Pro, Gemini 2.0 Flash
- **특이사항**:
  - Free tier 제공 (일 1,000 요청)
  - Context >200K 토큰 시 2x 가격 (Pro 모델)
  - Batch API 50% 할인

### xAI (Grok)
- **URL**: https://docs.x.ai/docs/models
- **API**: https://x.ai/api
- **주요 모델**: Grok 4, Grok 3 시리즈
- **특이사항**:
  - Live Search 기능 별도 과금 ($25/1,000 sources)
  - Web/X Search 도구 호출 별도 과금 ($2.50-$5/1,000 calls)
  - OpenAI API 호환 형식

### Microsoft Azure
- **URL**: https://azure.microsoft.com/en-us/pricing/details/cognitive-services/openai-service/
- **주요 모델**: Azure GPT-4o, Azure GPT-4o-mini
- **특이사항**:
  - Global 배포 vs Regional 배포 가격 차이
  - PTU(Provisioned Throughput Units) 별도 가격 체계
  - Fine-tuned 모델 호스팅 비용 ($2.52-$3.00/hour)

## 업데이트 절차

### 1. 가격 정보 수집

각 공급자의 공식 페이지에서 최신 가격을 확인합니다:

```markdown
## 체크리스트
- [ ] OpenAI - GPT-4o, GPT-4o-mini, o1, o3 시리즈
- [ ] Anthropic - Claude 4.5, Claude 3.5 시리즈
- [ ] Google - Gemini 2.5, Gemini 2.0 시리즈
- [ ] xAI - Grok 4, Grok 3 시리즈
- [ ] Azure - Azure OpenAI 모델들
```

### 2. ModelPricing.cs 수정

`src/TokenMeter/ModelPricing.cs` 파일을 수정합니다:

```csharp
// 1. LastUpdated 날짜 업데이트
public static DateOnly LastUpdated { get; } = new(2026, 2, 15);

// 2. 가격 정보 수정
["gpt-4o"] = new()
{
    ModelId = "gpt-4o",
    InputPricePerMillion = 2.50m,  // 새 가격으로 수정
    OutputPricePerMillion = 10.00m,
    Provider = "OpenAI",
    DisplayName = "GPT-4o",
    ContextWindow = 128000
},

// 3. 새 모델 추가 시
["new-model-id"] = new()
{
    ModelId = "new-model-id",
    InputPricePerMillion = X.XXm,
    OutputPricePerMillion = X.XXm,
    Provider = "ProviderName",
    DisplayName = "Display Name",
    ContextWindow = XXXXX
},
```

### 3. FindPricing 메서드 업데이트

새 모델 추가 시 패턴 매칭 로직도 업데이트합니다:

```csharp
// 새 모델 패턴 추가
if (normalized.Contains("new-model", StringComparison.Ordinal))
{
    return ProviderDict["new-model-id"];
}
```

### 4. README.md 업데이트

가격표를 최신 정보로 업데이트합니다.

### 5. 테스트 실행

```bash
cd submodules/TokenMeter
dotnet test
```

### 6. 변경 사항 커밋

```bash
git add .
git commit -m "chore: update model pricing (YYYY-MM-DD)

- OpenAI: [변경 내용]
- Anthropic: [변경 내용]
- Google: [변경 내용]
- xAI: [변경 내용]
- Azure: [변경 내용]

Sources:
- https://openai.com/api/pricing/
- https://docs.anthropic.com/en/docs/about-claude/pricing
- https://ai.google.dev/gemini-api/docs/pricing
- https://docs.x.ai/docs/models
- https://azure.microsoft.com/pricing/details/cognitive-services/openai-service/"
```

## 새 공급자 추가

### 1. 가격 Dictionary 추가

```csharp
/// <summary>
/// Gets pricing for NewProvider models.
/// </summary>
public static IReadOnlyDictionary<string, ModelPricing> NewProvider { get; } =
    new Dictionary<string, ModelPricing>(StringComparer.OrdinalIgnoreCase)
{
    ["model-1"] = new()
    {
        ModelId = "model-1",
        InputPricePerMillion = X.XXm,
        OutputPricePerMillion = X.XXm,
        Provider = "NewProvider",
        DisplayName = "Model 1"
    }
};
```

### 2. All Dictionary에 추가

```csharp
public static IReadOnlyDictionary<string, ModelPricing> All { get; } =
    OpenAI
        .Concat(Anthropic)
        .Concat(Google)
        .Concat(XAI)
        .Concat(Azure)
        .Concat(NewProvider)  // 추가
        .ToDictionary(kv => kv.Key, kv => kv.Value, StringComparer.OrdinalIgnoreCase);
```

### 3. ByProvider에 추가

```csharp
public static IReadOnlyDictionary<string, IReadOnlyDictionary<string, ModelPricing>> ByProvider { get; } =
    new Dictionary<string, IReadOnlyDictionary<string, ModelPricing>>(StringComparer.OrdinalIgnoreCase)
    {
        ["OpenAI"] = OpenAI,
        ["Anthropic"] = Anthropic,
        ["Google"] = Google,
        ["xAI"] = XAI,
        ["Azure"] = Azure,
        ["NewProvider"] = NewProvider  // 추가
    };
```

### 4. FindPricing 패턴 추가

```csharp
// NewProvider patterns
if (normalized.Contains("newprovider-model", StringComparison.Ordinal))
{
    return NewProvider["model-1"];
}
```

## 가격 변동 알림 설정 (선택사항)

주요 공급자의 가격 변동을 모니터링하려면:

1. **OpenAI**: https://status.openai.com/ RSS 구독
2. **Anthropic**: https://status.anthropic.com/ RSS 구독
3. **Google**: Cloud 알림 설정
4. **xAI**: X(Twitter) @xaboratory 팔로우

## 참고: 가격 비교 사이트

자동화된 가격 비교를 위해 다음 사이트를 참고할 수 있습니다:

- https://pricepertoken.com/ - 실시간 토큰 가격 비교
- https://artificialanalysis.ai/ - LLM 벤치마크 및 가격
- https://llmpricecheck.com/ - 가격 계산기

## 버전 히스토리

| 날짜 | 버전 | 변경 내용 |
|------|------|----------|
| 2026-01-28 | 0.1.0 | 초기 가격 데이터 (OpenAI, Anthropic, Google, xAI, Azure) |

---

Last Updated: 2026-01-28
