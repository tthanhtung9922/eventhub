# EventHub

> Nền tảng đặt vé sự kiện xây dựng theo kiến trúc **Modular Monolith** trên **.NET 10**. Mục tiêu: mỗi kỹ thuật backend cốt lõi (Authentication, Realtime, Caching, CDN, Messaging, Concurrency) có một lát cắt chạy thật, tối giản nhưng làm đúng cách.

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](./LICENSE)
[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4)](https://dotnet.microsoft.com/)

---

## Mục lục

- [EventHub](#eventhub)
  - [Mục lục](#mục-lục)
  - [Tổng quan](#tổng-quan)
  - [Trạng thái hiện tại](#trạng-thái-hiện-tại)
  - [Kỹ thuật được trình diễn](#kỹ-thuật-được-trình-diễn)
  - [Kiến trúc](#kiến-trúc)
  - [Tech Stack](#tech-stack)
  - [Bắt đầu nhanh](#bắt-đầu-nhanh)
    - [Yêu cầu](#yêu-cầu)
    - [Chạy](#chạy)
  - [Cấu trúc dự án](#cấu-trúc-dự-án)
  - [Kiểm thử](#kiểm-thử)
  - [Quyết định kiến trúc](#quyết-định-kiến-trúc)
  - [Lộ trình](#lộ-trình)
  - [License](#license)

---

## Tổng quan

EventHub cho phép Organizer tạo sự kiện và bán vé, Attendee đặt vé theo thời gian thực. Project được thiết kế như một **bài trình diễn kỹ thuật**: thay vì nhiều tính năng, mỗi khái niệm backend quan trọng có một lát cắt chạy thật và được giải thích lý do thiết kế.

Điểm nhấn kỹ thuật dự kiến là bài toán **chống overselling** (nhiều người mua vé cuối cùng cùng lúc), giải bằng optimistic concurrency kết hợp transactional outbox. Phần này **chưa xây**, xem [Lộ trình](#lộ-trình).

---

## Trạng thái hiện tại

Đây là project cá nhân đang xây dở, không phải hệ thống production. README này phân biệt rõ **cái đã chạy** và **cái mới nằm trong kế hoạch**, để ai đọc repo không phải đoán.

**Đã xây (chạy được, có thể đọc code):**

- Bộ khung Modular Monolith: host `EventHub.Api` làm composition root, `IModule` để module tự đăng ký, DbContext tách riêng theo module.
- Module **Identity** đầy đủ 4 tầng (Domain / Application / Infrastructure / Api):
  - Đăng ký, đăng nhập, JWT access token.
  - Refresh token rotation, phát hiện tái sử dụng token đã thu hồi thì revoke toàn bộ chain của user. Token sinh bằng RNG, lưu SHA-256 hash, unique index trên `TokenHash`.
  - Seed role Admin/User và admin mặc định qua `IHostedService`, credential đọc từ config.
  - Endpoint: `/identity/register`, `/login`, `/refresh`, `/logout`, `/me`, `/admin-only`.
  - Đã qua một vòng security review và vá 9 lỗ hổng (lockout, cân timing khi login, rotation atomic bằng `ExecuteUpdateAsync`, `ClockSkew = 0`, pin thuật toán HS256).
- `Result<T>` / `Error` / `ErrorType` ở SharedKernel, `GlobalExceptionHandler` trả ProblemDetails không lộ stack trace, `ValidationFilter<T>` + FluentValidation chặn input rác ở endpoint.
- Docker Compose cho hạ tầng phụ thuộc: PostgreSQL 17, Redis 8, MinIO.
- 5 ADR ghi lại các quyết định lớn.

**Chưa xây:**

Module Events, module Ticketing, Wolverine, SignalR, HybridCache (Redis mới chỉ có container, chưa có code dùng), tích hợp MinIO, chống overselling, OpenAPI/Scalar, Serilog, OpenTelemetry, test (unit / integration / architecture), Dockerfile cho API, CI trên GitHub Actions.

---

## Kỹ thuật được trình diễn

| Kỹ thuật                 | Module        | Cách triển khai                                                   | Trạng thái |
| ------------------------ | ------------- | ----------------------------------------------------------------- | ---------- |
| **Authentication**       | Identity      | JWT + refresh token rotation, phân quyền theo role                | Xong       |
| **Xử lý lỗi**            | Toàn hệ thống | `Result<T>` + ProblemDetails + FluentValidation                   | Xong       |
| **CRUD + Database**      | Events        | EF Core 10, pagination, validation                                | Kế hoạch   |
| **Caching**              | Events        | HybridCache (L1 in-memory + L2 Redis), cache-aside + invalidation | Kế hoạch   |
| **CDN / Object Storage** | Events        | Upload poster lên MinIO (S3-compatible)                           | Kế hoạch   |
| **Realtime**             | Ticketing     | SignalR: cập nhật số vé còn lại trực tiếp                         | Kế hoạch   |
| **Messaging / Queue**    | Ticketing     | Wolverine: đặt vé bất đồng bộ + transactional outbox              | Kế hoạch   |
| **Concurrency**          | Ticketing     | Optimistic concurrency (rowversion) chống overselling             | Kế hoạch   |
| **Observability**        | Toàn hệ thống | Serilog structured logging + OpenTelemetry tracing                | Kế hoạch   |
| **DevOps**               | Toàn hệ thống | Docker multi-stage, Compose, GitHub Actions CI                    | Kế hoạch   |

---

## Kiến trúc

EventHub là một **Modular Monolith**: một process duy nhất, mã nguồn chia thành các module độc lập. Mỗi module tự chứa Domain, Application, Infrastructure và API endpoints; các module giao tiếp với nhau **chỉ qua integration events** hoặc public contracts, không reference trực tiếp nội bộ của nhau.

Hiện mới có module **Identity**. Events và Ticketing nằm trong sơ đồ dưới đây là phần dự kiến, chưa có code.

```text
┌─────────────────────────────────────────────┐
│            EventHub.Api (Host)               │
│           Composition Root                   │
├───────────┬───────────────┬─────────────────┤
│  Identity │     Events     │   Ticketing     │
│  (xong)   │  (kế hoạch)    │   (kế hoạch)    │
└───────────┴───────────────┴─────────────────┘
        │            │              │
        └──── message bus (kế hoạch) ┘
              (integration events)
                     │
   ┌─────────┬───────┴────────┬──────────┐
PostgreSQL   Redis           MinIO     SignalR
 (đang dùng) (kế hoạch)   (kế hoạch)  (kế hoạch)
```

Ranh giới module dự kiến sẽ được ép tự động bằng architecture test (NetArchTest) chạy trong CI. Cả hai phần này chưa làm.

Lý do lựa chọn xem trong các [ADR](docs/adr/).

---

## Tech Stack

Đang dùng thật:

| Lớp        | Công nghệ                                       |
| ---------- | ----------------------------------------------- |
| Runtime    | .NET 10, C# 14                                  |
| Web        | ASP.NET Core 10 (Minimal API)                   |
| ORM / DB   | EF Core 10, PostgreSQL 17 (Npgsql)              |
| Auth       | ASP.NET Core Identity + JWT Bearer              |
| Validation | FluentValidation                                |
| Hạ tầng    | Docker Compose (PostgreSQL, Redis, MinIO)       |

Dự kiến thêm theo lộ trình: Wolverine, HybridCache + Redis, SignalR, MinIO client, Mapster, Serilog, OpenTelemetry, OpenAPI/Scalar, xUnit + NSubstitute + Shouldly + Testcontainers + NetArchTest, GitHub Actions.

> **Lưu ý về license:** project chủ động tránh các thư viện đã chuyển sang license thương mại từ 2025 (MediatR, AutoMapper, MassTransit, Moq, FluentAssertions) và chọn thay thế tương đương. Lý do chi tiết trong [ADR-0003](docs/adr/0003-tranh-thu-vien-thuong-mai.md).

---

## Bắt đầu nhanh

### Yêu cầu

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- [Docker](https://www.docker.com/) và Docker Compose

### Chạy

API chưa được đóng gói vào Docker, nên compose chỉ dựng hạ tầng phụ thuộc, còn API chạy từ source.

```bash
# Clone repo
git clone https://github.com/tthanhtung9922/eventhub.git
cd eventhub

# Tạo .env ở gốc từ mẫu
cp .env.example .env

# Dựng hạ tầng: PostgreSQL + Redis + MinIO
docker compose -f docker/docker-compose.yml --env-file .env up -d

# Chạy API từ source
dotnet run --project src/Bootstrap/EventHub.Api

# MinIO console: http://localhost:9001
```

Muốn kèm pgAdmin thì dùng `docker/docker-compose.local.yml`.

---

## Cấu trúc dự án

```text
EventHub/
├── src/
│   ├── Bootstrap/EventHub.Api/      # Host duy nhất, composition root
│   ├── Modules/
│   │   └── Identity/                # Auth, JWT, refresh token rotation
│   │       ├── EventHub.Identity.Domain/
│   │       ├── EventHub.Identity.Application/
│   │       ├── EventHub.Identity.Infrastructure/
│   │       └── EventHub.Identity.Api/
│   └── Shared/
│       ├── EventHub.SharedKernel/   # Result<T>, Error, ErrorType
│       ├── EventHub.Modularity/     # IModule, ResultExtensions
│       └── EventHub.Contracts/      # Integration events giữa các module
├── docker/                          # Compose + cấu hình
└── docs/                            # ROADMAP + ADR
```

Module Events, Ticketing và thư mục `tests/` sẽ thêm theo [lộ trình](docs/ROADMAP.md).

---

## Kiểm thử

Chưa có test. Đây là khoản nợ lớn nhất của repo hiện tại.

Kế hoạch ba tầng test khi làm tới:

- **Unit test**: logic domain và handler, dùng NSubstitute để mock.
- **Integration test**: chạy với PostgreSQL và Redis thật qua Testcontainers.
- **Architecture test**: NetArchTest ép ranh giới giữa các module, vi phạm thì fail CI.

---

## Quyết định kiến trúc

Các quyết định lớn được ghi lại dưới dạng ADR (Architecture Decision Record):

- [ADR-0001: Dùng Modular Monolith thay vì microservices](docs/adr/0001-modular-monolith.md)
- [ADR-0002: Dùng Wolverine làm mediator + message bus + transactional outbox](docs/adr/0002-wolverine.md)
- [ADR-0003: Tránh thư viện đã thương mại hóa, chọn Mapster / NSubstitute / Shouldly](docs/adr/0003-tranh-thu-vien-thuong-mai.md)
- [ADR-0004: Ranh giới module Identity theo Option A (Dependency Inversion / IIdentityService)](docs/adr/0004-identity-option-a.md)
- [ADR-0005: Phát JWT bằng short-name claim với IdentityClaimTypes làm source of truth](docs/adr/0005-jwt-short-name-claim.md)

ADR-0002 ghi quyết định chọn Wolverine, nhưng phần tích hợp thật vẫn chưa làm.

---

## Lộ trình

Lộ trình phát triển chi tiết 4 tuần xem trong [docs/ROADMAP.md](docs/ROADMAP.md).

- [ ] **Tuần 1**: Nền móng, solution, Identity (auth), Events (CRUD)
  - [x] Nền móng, solution, bộ khung module
  - [x] Identity: auth, JWT, refresh token rotation
  - [ ] Events: CRUD
- [ ] **Tuần 2**: Caching & CDN, HybridCache, invalidation, MinIO
- [ ] **Tuần 3**: Realtime & Messaging, SignalR, Wolverine outbox, chống overselling
- [ ] **Tuần 4**: DevOps & hoàn thiện, Docker, CI/CD, observability, docs

---

## License

Dự án này được phát hành dưới [MIT License](./LICENSE).
