export const WEB_ACCESS_MESSAGE = 'Web access is available only to Backoffice and Grid Operator accounts. Prosumers should use the Android app.'

export function canAccessWeb(role: string): boolean {
  return role === 'BACKOFFICE' || role === 'GRID_OPERATOR'
}

export class WebAccessError extends Error {
  constructor() {
    super(WEB_ACCESS_MESSAGE)
  }
}
