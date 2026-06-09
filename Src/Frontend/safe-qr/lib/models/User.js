/**
 * User Model
 * Represents an authenticated user of the Safe QR application.
 */
export class User {
  constructor({ userId = null, fullName = '', email = '', role = 'user' } = {}) {
    this.userId   = userId;
    this.fullName = fullName;
    this.email    = email;
    this.role     = role;
  }

  /** Returns the user's initials for the avatar badge. */
  getInitials() {
    return this.fullName
      .split(' ')
      .map(w => w[0])
      .join('')
      .toUpperCase()
      .slice(0, 2) || '?';
  }

  /** Returns true if the user has administrator privileges. */
  isAdmin() {
    return this.role === 'admin';
  }

  /** Returns a display-friendly first name. */
  getFirstName() {
    return this.fullName.split(' ')[0];
  }
}
