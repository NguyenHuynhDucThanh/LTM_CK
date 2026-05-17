let socket;
let me = null;
let currentRoom = null;
let questionTimer = null;

const $ = (id) => document.getElementById(id);

$("joinBtn").onclick = () => {
  socket = new WebSocket(`ws://${location.host}/ws`);
  socket.onopen = () => send("ENTER_NICKNAME", { nickname: $("nickname").value });
  socket.onmessage = (event) => handle(JSON.parse(event.data));
  socket.onclose = () => alert("Mất kết nối server");
};

$("createRoomBtn").onclick = () => {
  send("CREATE_ROOM", {
    roomName: $("roomName").value,
    questionSet: $("questionSet").value
  });
};

$("leaveBtn").onclick = () => {
  send("LEAVE_ROOM", {});
  currentRoom = null;
  resetRoomUi();
  show("lobby");
};

$("startBtn").onclick = () => send("START_GAME", {});
$("sendChatBtn").onclick = sendChat;
$("chatInput").onkeydown = (e) => {
  if (e.key === "Enter") sendChat();
};

setInterval(() => {
  if (socket?.readyState === WebSocket.OPEN) send("HEARTBEAT", {});
}, 10000);

function send(type, data) {
  socket.send(JSON.stringify({
    type,
    data,
    timestamp: Date.now(),
    requestId: crypto.randomUUID()
  }));
}

function handle(message) {
  const { type, data } = message;

  if (type === "ERROR") alert(data.message);

  if (type === "SESSION_CREATED") {
    me = data.player;
    $("me").textContent = `Bạn: ${me.nickname}`;
    show("lobby");
  }

  if (type === "LOBBY_UPDATED") renderLobby(data);
  if (type === "ROOM_UPDATED") renderRoom(data);
  if (type === "CHAT") addChat(data);
  if (type === "QUESTION") renderQuestion(data);
  if (type === "SCOREBOARD") renderScoreboard(data.scoreboard);
  if (type === "RESULT") {
    stopQuestionTimer();
    $("gameInfo").textContent = `Đáp án đúng: ${data.correct}`;
    renderScoreboard(data.scoreboard);
  }
  if (type === "GAME_OVER") {
    stopQuestionTimer();
    $("question").textContent = "Game over";
    $("answers").innerHTML = "";
    renderScoreboard(data.scoreboard);
  }
}

function renderLobby(data) {
  $("questionSet").innerHTML = data.questionSets
    .map(name => `<option value="${name}">${name}</option>`)
    .join("");

  $("players").innerHTML = data.players
    .map(p => `<div class="player-item">${p.nickname}</div>`)
    .join("");

  $("rooms").innerHTML = data.rooms.map(room => `
    <div class="room-item">
      <b>${room.name}</b><br>
      Bộ câu hỏi: ${room.questionSetName}<br>
      Trạng thái: ${room.status}<br>
      Người chơi: ${room.players.length}
      <button onclick="joinRoom('${room.id}')">Join</button>
    </div>
  `).join("");
}

function joinRoom(roomId) {
  send("JOIN_ROOM", { roomId });
}

function renderRoom(room) {
  const isNewRoom = !currentRoom || currentRoom.id !== room.id;
  currentRoom = room;
  show("room");
  $("roomTitle").textContent = `${room.name} - ${room.status}`;
  $("roomPlayers").innerHTML = room.players
    .map(p => `<div class="player-item">${p.nickname}${p.id === room.hostId ? " (Host)" : ""}</div>`)
    .join("");

  $("startBtn").disabled = me.id !== room.hostId || room.status !== "WAITING";

  if (isNewRoom) {
    resetRoomUi();
  }
}

function resetRoomUi() {
  stopQuestionTimer();
  $("gameInfo").textContent = "";
  $("question").textContent = "Chờ host bắt đầu...";
  $("answers").innerHTML = "";
  $("scoreboard").innerHTML = "";
  $("chatLog").innerHTML = "";
  $("chatInput").value = "";
}

function renderQuestion(data) {
  startQuestionTimer(data.index, data.total, data.timeLimit);
  $("question").textContent = data.question.content;
  $("answers").innerHTML = ["A", "B", "C", "D"].map(key => `
    <button class="answer-btn" onclick="answer('${key}')">${key}. ${data.question[key.toLowerCase()] ?? data.question[key]}</button>
  `).join("");
}

function startQuestionTimer(index, total, timeLimit) {
  stopQuestionTimer();

  let remaining = timeLimit;
  updateQuestionTimer(index, total, remaining);

  questionTimer = setInterval(() => {
    remaining -= 1;
    updateQuestionTimer(index, total, remaining);

    if (remaining <= 0) {
      stopQuestionTimer();
      $("answers").querySelectorAll("button").forEach(btn => btn.disabled = true);
    }
  }, 1000);
}

function updateQuestionTimer(index, total, remaining) {
  $("gameInfo").textContent = `Câu ${index}/${total} - Còn ${Math.max(0, remaining)}s`;
}

function stopQuestionTimer() {
  if (questionTimer) {
    clearInterval(questionTimer);
    questionTimer = null;
  }
}

function answer(value) {
  send("ANSWER", { answer: value });
  $("answers").querySelectorAll("button").forEach(btn => btn.disabled = true);
}

function sendChat() {
  const input = $("chatInput");
  if (!input.value.trim()) return;
  send("CHAT", { text: input.value });
  input.value = "";
}

function addChat(data) {
  $("chatLog").innerHTML += `<div class="chat-item">[${data.time}] <b>${data.player}</b>: ${data.text}</div>`;
  $("chatLog").scrollTop = $("chatLog").scrollHeight;
}

function renderScoreboard(scoreboard) {
  $("scoreboard").innerHTML = "<h3>Scoreboard</h3>" + scoreboard
    .map(p => `<div class="score-item">${p.nickname}: ${p.score}</div>`)
    .join("");
}

function show(id) {
  ["login", "lobby", "room"].forEach(screen => $(screen).classList.add("hidden"));
  $(id).classList.remove("hidden");
}
