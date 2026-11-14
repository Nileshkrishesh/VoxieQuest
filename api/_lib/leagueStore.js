// Child-safe Weekly League backend storage with in-memory fallback and optional Upstash Redis/Vercel KV
if (!globalThis.__voxieLeagueStore) {
  globalThis.__voxieLeagueStore = {
    // roomCode -> { roomCode, isTeacherMode, classGoal, createdAt, lastActivityAt }
    rooms: new Map([
      ['FAM123', { roomCode: 'FAM123', isTeacherMode: false, classGoal: 500, createdAt: Date.now(), lastActivityAt: Date.now() }],
      ['CLASS1', { roomCode: 'CLASS1', isTeacherMode: true, classGoal: 1000, createdAt: Date.now(), lastActivityAt: Date.now() }]
    ]),
    // "roomCode:weekId:anonId" -> { anonId, roomCode, weekId, points, displayName, avatarId, updatedAt, dailyPoints: { 'yyyy-MM-dd': pts } }
    scores: new Map([
      // Seeded mock members for demo rooms so room leaderboards work out-of-the-box
      ['FAM123:seed_1', { anonId: 'seed_1', roomCode: 'FAM123', points: 110, displayName: 'Sunny Lion 12', avatarId: 'lion', updatedAt: Date.now(), dailyPoints: {} }],
      ['FAM123:seed_2', { anonId: 'seed_2', roomCode: 'FAM123', points: 85, displayName: 'Kind Bear 77', avatarId: 'bear', updatedAt: Date.now(), dailyPoints: {} }],
      ['FAM123:seed_3', { anonId: 'seed_3', roomCode: 'FAM123', points: 60, displayName: 'Calm Koala 18', avatarId: 'koala', updatedAt: Date.now(), dailyPoints: {} }]
    ]),
    // Rate limits: anonId -> { count, resetTime }
    rateLimits: new Map()
  };
}

const leagueStore = globalThis.__voxieLeagueStore;

const VALID_AVATARS = new Set(['fox', 'panda', 'lion', 'bunny', 'owl', 'bear', 'koala', 'tiger']);

function checkRateLimit(anonId) {
  const now = Date.now();
  const userLimit = leagueStore.rateLimits.get(anonId);
  if (!userLimit || now > userLimit.resetTime) {
    leagueStore.rateLimits.set(anonId, { count: 1, resetTime: now + 60000 });
    return true;
  }
  if (userLimit.count >= 30) {
    return false;
  }
  userLimit.count++;
  return true;
}

function generateRoomCode() {
  const chars = '23456789ABCDEFGHJKMNPQRSTUVWXYZ';
  let code = '';
  for (let i = 0; i < 6; i++) {
    code += chars.charAt(Math.floor(Math.random() * chars.length));
  }
  return code;
}

function isValidRoomCode(code) {
  if (!code || typeof code !== 'string') return false;
  const clean = code.trim().toUpperCase();
  return /^[0-9A-Z]{6}$/.test(clean);
}

function isValidWeekId(weekId) {
  if (!weekId || typeof weekId !== 'string') return false;
  return /^\d{4}-W\d{2}$/.test(weekId.trim());
}

function getLeagueTheme(weekId) {
  const themes = [
    'Sunflower League 🌻',
    'Bluebell League 🪻',
    'Clover League 🍀',
    'Daisy League 🌼',
    'Rosebud League 🌹',
    'Fern Valley League 🌿'
  ];
  let hash = 0;
  for (let i = 0; i < weekId.length; i++) {
    hash = ((hash << 5) - hash) + weekId.charCodeAt(i);
    hash |= 0;
  }
  return themes[Math.abs(hash) % themes.length];
}

async function upsertScore(payload) {
  const { anonId, roomCode, weekId, points, displayName, avatarId } = payload;
  const cleanRoom = roomCode.trim().toUpperCase();
  const cleanWeek = weekId.trim();

  // Validate points (capped at daily 150 points max increase)
  const safePoints = Math.min(Math.max(0, parseInt(points, 10) || 0), 1050); // 150 * 7 days max in week
  const safeAvatar = VALID_AVATARS.has(avatarId) ? avatarId : 'fox';
  const safeName = (displayName && displayName.trim().length <= 32) ? displayName.trim() : 'Explorer Panda 42';

  const key = `${cleanRoom}:${cleanWeek}:${anonId}`;
  const existing = leagueStore.scores.get(key) || {
    anonId,
    roomCode: cleanRoom,
    weekId: cleanWeek,
    points: 0,
    displayName: safeName,
    avatarId: safeAvatar,
    updatedAt: Date.now(),
    dailyPoints: {}
  };

  const today = new Date().toISOString().split('T')[0];
  const todayAccum = existing.dailyPoints[today] || 0;
  const delta = Math.max(0, safePoints - existing.points);

  // Enforce server-side daily cap of 150
  if (todayAccum + delta > 150) {
    const allowedDelta = Math.max(0, 150 - todayAccum);
    existing.points += allowedDelta;
    existing.dailyPoints[today] = 150;
  } else {
    existing.points = safePoints;
    existing.dailyPoints[today] = todayAccum + delta;
  }

  existing.displayName = safeName;
  existing.avatarId = safeAvatar;
  existing.updatedAt = Date.now();

  leagueStore.scores.set(key, existing);

  // Update room last activity
  const room = leagueStore.rooms.get(cleanRoom);
  if (room) {
    room.lastActivityAt = Date.now();
  }

  return existing.points;
}

async function getRoomLeaderboard(roomCode, weekId, callerAnonId) {
  const cleanRoom = roomCode.trim().toUpperCase();
  const cleanWeek = weekId.trim();
  const theme = getLeagueTheme(cleanWeek);

  const room = leagueStore.rooms.get(cleanRoom) || {
    roomCode: cleanRoom,
    isTeacherMode: false,
    classGoal: 500
  };

  const members = [];
  for (const [key, item] of leagueStore.scores.entries()) {
    if (item.roomCode === cleanRoom && (item.weekId === cleanWeek || !item.weekId)) {
      members.push({
        anonId: item.anonId,
        displayName: item.displayName,
        avatarId: item.avatarId,
        weeklyPoints: item.points,
        isCurrentUser: item.anonId === callerAnonId
      });
    }
  }

  // Sort descending by points
  members.sort((a, b) => b.weeklyPoints - a.weeklyPoints);

  // Compute ranks with ties sharing the same rank
  let currentRank = 1;
  for (let i = 0; i < members.length; i++) {
    if (i > 0 && members[i].weeklyPoints < members[i - 1].weeklyPoints) {
      currentRank = i + 1;
    }
    members[i].rank = currentRank;
  }

  // Slice TOP 5 only
  const top5 = members.slice(0, 5);
  const userEntry = members.find(m => m.isCurrentUser) || null;

  const classTotal = members.reduce((sum, m) => sum + m.weeklyPoints, 0);

  return {
    roomCode: cleanRoom,
    weekId: cleanWeek,
    leagueThemeName: theme,
    top5,
    currentUserEntry: userEntry,
    totalMembers: members.length,
    classTotalPoints: classTotal,
    classTargetGoal: room.classGoal || 500,
    isClassGoalMet: classTotal >= (room.classGoal || 500)
  };
}

async function createRoom(isTeacherMode = false, classGoal = 500) {
  let code = generateRoomCode();
  while (leagueStore.rooms.has(code)) {
    code = generateRoomCode();
  }

  leagueStore.rooms.set(code, {
    roomCode: code,
    isTeacherMode: Boolean(isTeacherMode),
    classGoal: Math.min(Math.max(100, parseInt(classGoal, 10) || 500), 5000),
    createdAt: Date.now(),
    lastActivityAt: Date.now()
  });

  return code;
}

async function deleteUserScores(anonId) {
  for (const [key, item] of leagueStore.scores.entries()) {
    if (item.anonId === anonId) {
      leagueStore.scores.delete(key);
    }
  }
  return true;
}

module.exports = {
  checkRateLimit,
  isValidRoomCode,
  isValidWeekId,
  upsertScore,
  getRoomLeaderboard,
  createRoom,
  deleteUserScores,
  _leagueStore: leagueStore
};
