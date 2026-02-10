# LLM Pricing Update Guide

이 문서는 TokenMeter의 모델 가격 정보를 주기적으로 업데이트하기 위한 가이드입니다.

## 업데이트 주기

- **권장 주기**: 월 1회 또는 주요 모델 출시 시
- **긴급 업데이트**: 가격 정책 변경 발표 시 즉시

## 공식 가격 정보 소스

| Provider | URL |
|----------|-----|
| OpenAI | https://openai.com/api/pricing/ |
| Anthropic | https://docs.anthropic.com/en/docs/about-claude/pricing |
| Google | https://ai.google.dev/gemini-api/docs/pricing |
| xAI | https://docs.x.ai/docs/models |
| Azure | https://azure.microsoft.com/en-us/pricing/details/cognitive-services/openai-service/ |
| Mistral | https://mistral.ai/technology/ |
| DeepSeek | https://platform.deepseek.com/api-docs/pricing |
| Amazon Nova | https://aws.amazon.com/bedrock/pricing/ |
| Cohere | https://cohere.com/pricing |
| Meta Llama | https://llama.meta.com/ |
| Perplexity | https://docs.perplexity.ai/guides/pricing |
| Qwen | https://help.aliyun.com/zh/model-studio/ |

## 업데이트 절차

### 1. 가격 정보 수집

각 공급자의 공식 페이지에서 최신 가격을 확인합니다.

### 2. JSON 파일 수정

`src/TokenMeter/Pricing/` 디렉토리에서 해당 프로바이더의 JSON 파일을 편집합니다.

**기존 모델 가격 변경:**

```json
{
  "modelId": "gpt-4o",
  "inputPricePerMillion": 2.50,
  "outputPricePerMillion": 10.00,
  "displayName": "GPT-4o",
  "contextWindow": 128000,
  "aliases": [
    { "pattern": "gpt-4o", "type": "prefix" }
  ]
}
```

**새 모델 추가:**

JSON 파일의 `models` 배열에 새 항목을 추가합니다.
**중요**: 모델 순서가 alias 매칭 우선순위를 결정합니다. 더 구체적인 패턴의 모델을 먼저 배치하세요.

예시: `gpt-4o-mini`를 `gpt-4o` 보다 먼저 배치 (prefix "gpt-4o"가 "gpt-4o-mini"도 매칭하므로)

**Alias 타입:**

| Type | 설명 | 예시 |
|------|------|------|
| `exact` | 정확히 일치 | `"gpt-4o"` → `gpt-4o`만 매칭 |
| `prefix` | 접두사 일치 | `"gpt-4o"` → `gpt-4o`, `gpt-4o-2024-08-06` 매칭 |
| `contains` | 부분 문자열 일치 | `"claude-3.5-sonnet"` → 어디든 포함되면 매칭 |

### 3. 새 프로바이더 추가

`src/TokenMeter/Pricing/` 디렉토리에 새 JSON 파일을 생성합니다:

```json
{
  "provider": "NewProvider",
  "models": [
    {
      "modelId": "model-id",
      "inputPricePerMillion": 1.00,
      "outputPricePerMillion": 2.00,
      "displayName": "Model Name",
      "contextWindow": 128000,
      "aliases": [
        { "pattern": "model", "type": "prefix" }
      ]
    }
  ]
}
```

JSON 파일을 추가하면 Embedded Resource로 자동 포함됩니다 (`TokenMeter.csproj`의 `Pricing\*.json` 와일드카드).

`ModelPricingData`에 프로바이더 프로퍼티를 추가하려면 `ModelPricing.cs`에 다음을 추가:

```csharp
public static IReadOnlyDictionary<string, ModelPricing> NewProvider => GetProviderDict("NewProvider");
```

### 4. LastUpdated 날짜 업데이트

`ModelPricing.cs`에서 `LastUpdated` 날짜를 업데이트합니다.

### 5. README.md 업데이트

가격표를 최신 정보로 업데이트합니다.

### 6. 테스트 실행

```bash
cd D:\data\TokenMeter
dotnet test tests/TokenMeter.Tests/TokenMeter.Tests.csproj
```

### 7. 변경 사항 커밋

```bash
git add .
git commit -m "chore: update model pricing (YYYY-MM-DD)

- [변경 내용 요약]

Sources:
- [참조 URL]"
```

## 가격 변동 알림 설정 (선택사항)

1. **OpenAI**: https://status.openai.com/ RSS 구독
2. **Anthropic**: https://status.anthropic.com/ RSS 구독
3. **Google**: Cloud 알림 설정
4. **xAI**: X(Twitter) @xaboratory 팔로우

## 참고: 가격 비교 사이트

- https://pricepertoken.com/ - 실시간 토큰 가격 비교
- https://artificialanalysis.ai/ - LLM 벤치마크 및 가격
- https://llmpricecheck.com/ - 가격 계산기

## 버전 히스토리

| 날짜 | 버전 | 변경 내용 |
|------|------|----------|
| 2026-02-10 | 0.3.0 | JSON 기반 가격 시스템으로 리팩토링, 12개 프로바이더 지원 |
| 2026-01-28 | 0.1.0 | 초기 가격 데이터 (OpenAI, Anthropic, Google, xAI, Azure) |

---

Last Updated: 2026-02-10
