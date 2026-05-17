# QuizNet+

QuizNet+ là hệ thống Quiz Game nhiều người chơi realtime theo mô hình Client-Server, giao tiếp thời gian thực bằng WebSocket.

## Công nghệ

- Backend: C# ASP.NET Core .NET 8
- Frontend: HTML, CSS, JavaScript
- Realtime: WebSocket
- Runtime storage: in-memory collections (ConcurrentDictionary, List)
- Question sets: JSON files

## Cấu trúc dự án

```text
QuizNet/
├── server/
│   ├── Managers/
│   │   ├── SessionManager.cs
│   │   ├── RoomManager.cs
│   │   ├── QuestionManager.cs
│   │   └── GameManager.cs
│   ├── Models/
│   │   ├── Player.cs
│   │   ├── PlayerSession.cs
│   │   ├── Room.cs
│   │   ├── GameSession.cs
│   │   ├── Question.cs
│   │   └── Message.cs
│   ├── Services/
│   │   ├── HeartbeatService.cs
│   │   └── ScoreService.cs
│   ├── WebSocket/
│   │   ├── WebSocketHandler.cs
│   │   └── MessageTypes.cs
│   ├── Utils/
│   │   ├── InputValidator.cs
│   │   └── TextSanitizer.cs
│   ├── Questions/
│   │   ├── science.json
│   │   ├── history.json
│   │   └── anime.json
│   ├── wwwroot/
│   │   ├── index.html
│   │   ├── css/style.css
│   │   └── js/app.js
│   └── Program.cs
└── README.md
```

## Chạy project

```powershell
cd D:\LTM\QuizGame\server
dotnet run
```

Mở trình duyệt:

```text
http://localhost:8888
```

## Network

- HTTP Port: `8888`
- WebSocket Endpoint: `ws://localhost:8888/ws`
- Health Check: `http://localhost:8888/health`

## Chức năng

### Player Session
- Nhập nickname (2-20 ký tự, không trùng)
- Tạo temporary session
- Quản lý danh sách player online

### Lobby System
- Hiển thị danh sách phòng, player online, question sets
- Realtime update khi room được tạo/xóa, player join/leave

### Room Management
- Tạo phòng (chọn tên phòng + question set)
- Tham gia phòng
- Rời phòng
- Host migration khi host rời phòng ở trạng thái WAITING
- Tự động xóa room khi không còn player

### Gameplay
- Host nhấn Start Game để bắt đầu
- Broadcast câu hỏi realtime tới toàn bộ player
- Timer 20 giây mỗi câu hỏi
- Tất cả player trả lời đồng thời
- Tính điểm theo tốc độ: `Score = 100 + (20 - t) × 5`
- Scoreboard realtime sau mỗi câu trả lời
- Game over với bảng xếp hạng cuối cùng

### Chat System
- Chat realtime trong room
- Validate message (tối đa 200 ký tự)
- Sanitize HTML để chống XSS

### Heartbeat System
- Client gửi heartbeat mỗi 10 giây
- Server kiểm tra timeout mỗi 10 giây
- Tự động đóng kết nối nếu không nhận heartbeat trong 30 giây

### Console Logging
- Connection/Disconnect logs
- Room logs (tạo, vào, rời phòng)
- Game logs (start, câu hỏi, trả lời, kết thúc)
- Chat logs
- Heartbeat timeout logs
- Error logs

## Backend Modules

| Module | Chức năng |
|---|---|
| `WebSocketHandler` | Network gateway: nhận WebSocket, parse message, route command, broadcast |
| `SessionManager` | Quản lý player sessions (ConcurrentDictionary) |
| `RoomManager` | Tạo/join/leave room, host migration, room cleanup |
| `QuestionManager` | Load question sets từ JSON files |
| `GameManager` | Game loop, timer, answer validation, broadcast câu hỏi |
| `ScoreService` | Tính điểm theo độ đúng và tốc độ trả lời |
| `HeartbeatService` | Background service kiểm tra kết nối, kick player timeout |
| `InputValidator` | Validate nickname, room name, chat message |
| `TextSanitizer` | Sanitize HTML trong chat để chống XSS |
| `MessageTypes` | Hằng số cho các loại WebSocket message |

## Models

| Class | Mô tả |
|---|---|
| `Player` | Id, Nickname, Score |
| `PlayerSession` | SessionId, Player, Socket, CurrentRoomId, LastHeartbeat |
| `Room` | Id, Name, HostId, QuestionSetName, PlayerIds, GameSession |
| `GameSession` | State, CurrentIndex, CurrentQuestion, QuestionStartedAt, TimeLimitSeconds, Answers |
| `Question` | Content, A, B, C, D, Correct |
| `Message` | Type, Data (JsonElement), Timestamp, RequestId |

## Gameplay State Machine

```
WAITING → PLAYING → GAME_OVER → WAITING
```

## Session Lifecycle

```
CONNECT → ENTER_NICKNAME → JOIN_LOBBY → CREATE_ROOM/JOIN_ROOM → GAMEPLAY → DISCONNECT
```

## WebSocket Protocol

Tất cả message giữa FE ↔ BE sử dụng JSON:

```json
{
  "type": "COMMAND",
  "data": {},
  "timestamp": 123456,
  "requestId": "uuid"
}
```

### Message Types

| Type | Chiều | Chức năng |
|---|---|---|
| ENTER_NICKNAME | Client → Server | Đăng nhập nickname |
| SESSION_CREATED | Server → Client | Xác nhận session |
| LOBBY_UPDATED | Server → Client | Đồng bộ lobby |
| CREATE_ROOM | Client → Server | Tạo phòng |
| JOIN_ROOM | Client → Server | Vào phòng |
| LEAVE_ROOM | Client → Server | Rời phòng |
| ROOM_UPDATED | Server → Client | Đồng bộ room |
| START_GAME | Client → Server | Bắt đầu game |
| QUESTION | Server → Client | Gửi câu hỏi |
| ANSWER | Client → Server | Gửi câu trả lời |
| RESULT | Server → Client | Kết quả sau mỗi câu |
| SCOREBOARD | Server → Client | Bảng điểm realtime |
| GAME_OVER | Server → Client | Kết thúc game |
| CHAT | Hai chiều | Nhắn tin trong room |
| ERROR | Server → Client | Báo lỗi |
| HEARTBEAT | Client → Server | Kiểm tra kết nối |
| HEARTBEAT_ACK | Server → Client | Phản hồi heartbeat |

## Question Sets

- `science.json` – 18 câu (Mạng máy tính, CNTT)
- `history.json` – 18 câu (Lịch sử Việt Nam và thế giới)
- `anime.json` – 18 câu (Anime/Manga)
