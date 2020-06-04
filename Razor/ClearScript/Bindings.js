// @ts-check

export class Plugin {
  constructor() {}
  install() {}
  stop() {}
  start() {}
  uninstall() {}
}

export class UObject {
  constructor(nativeObj) {
    Object.defineProperty(this, "name", {
      get: () => nativeObj.Name,
      enumerable: true
    });
  }
}

export class Player extends UObject {
  constructor(nativeObj) {
    super(nativeObj);
    Object.defineProperty(this, "backpack", {
      get: () => new Item(nativeObj.backpack),
      enumerable: true
    });

    Object.defineProperty(this, "overhead", {
      value: function(msg) {
        debugger;
        nativeObj.OverheadMessage(msg);
      }
    });
  }
}

export class Item extends UObject {
  constructor(nativeObj) {
    super(nativeObj);
    Object.defineProperty(this, "contents", {
      get: () => Array.from(nativeObj.contents),
      enumerable: true
    });
  }
}

/** @var {any} player */


export function sleepAsync(duration) { 
  return new Promise(resolve => Timer.DelayedCallback(TimeSpan.FromMilliseconds(duration), new TimerCallback(resolve)).Start());
}

export function sleepSync(duration) { 
  const start = Date.now(); while (Date.now() - start < duration) { };
}

export function move(direction, { paces } = { paces: 1 } ) {
    for (var i = 0; i < paces; i++) 
    {
      player.move(direction);
      sleep(400);
    }
}

export function overhead(args, opts = null) {
  player.overhead(args, opts);
}