export function SolarIcon({ name, className = '' }: { name: string; className?: string }) {
  const paths: Record<string, string> = {
    Dashboard: 'M3 3h7v7H3z M14 3h7v7h-7z M3 14h7v7H3z M14 14h7v7h-7z',
    Users: 'M16 21v-2a4 4 0 0 0-4-4H6a4 4 0 0 0-4 4v2 M9 11a4 4 0 1 0 0-8 4 4 0 0 0 0 8 M17 4a4 4 0 0 1 0 8 M22 21v-2a4 4 0 0 0-3-3.87',
    Prosumers: 'M5 3h14v18H5z M12 7v10 M8 12h8',
    Stations: 'M5 3h12v18H5z M10 8l-2 5h5l-2 5 M20 9l-2 4h4l-2 4',
    Reservations: 'M4 5h16v16H4z M4 10h16 M8 2v6 M16 2v6',
    Transfers: 'M3 7h18l-5-5 M21 17H3l5 5',
    Review: 'M6 2h8l4 4v16H6z M14 2v5h4 M9 12h6 M9 16h6',
    Logout: 'M10 3H3v18h7 M8 12h13 M17 8l4 4-4 4',
    Arrow: 'M4 12h16 M14 6l6 6-6 6',
    Energy: 'M13 2L4 14h7l-1 8L20 9h-7z',
    Account: 'M4 21v-2a8 6 0 0 1 16 0v2 M12 11a4 4 0 1 0 0-8 4 4 0 0 0 0 8',
  }
  return <svg className={`solar-icon ${className}`} viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.7" strokeLinecap="round" strokeLinejoin="round" aria-hidden="true"><path d={paths[name] ?? paths.Reservations} /></svg>
}

export function SolarLandscape({ className = '' }: { className?: string }) {
  return <svg className={`solar-landscape ${className}`} viewBox="0 0 480 210" fill="none" aria-hidden="true">
    <circle cx="110" cy="43" r="16" fill="#FFE48C" />
    <path d="M110 12v-9m0 80v-9M79 43h-9m80 0h-9M88 21l-6-6m50 50l6 6M88 65l-6 6m50-50l6-6" stroke="#FFE48C" strokeWidth="3" />
    <path d="M0 160Q70 130 130 152Q220 66 302 110Q390 48 480 100V210H0z" fill="#E6E7F8" />
    <path d="M0 193Q110 134 210 179Q340 119 480 174V210H0z" fill="#DAEEE5" />
    <path d="M160 65q15-25 35 0q23-7 28 12h-78q0-12 15-12M43 115q10-18 25 0q20-5 25 10H27q2-10 16-10" fill="#E6E7F8" />
    <g fill="#B6DACB"><ellipse cx="410" cy="153" rx="13" ry="30"/><ellipse cx="453" cy="133" rx="17" ry="39"/><ellipse cx="85" cy="182" rx="10" ry="21"/></g>
    <path d="M410 160v41m43-62v62M85 184v20" stroke="#91BEAC" strokeWidth="3" />
    <path d="M145 147h102l-24 49H120z" fill="#AAA7E0" stroke="white" strokeWidth="3" />
    <path d="M139 160h101m-107 13h101m-108 12h101M170 147l-25 49m51-49l-25 49m52-49l-25 49" stroke="white" strokeWidth="2" />
    <path d="M154 197v11m53-11v11" stroke="#A0A9BC" strokeWidth="5" />
    <rect x="275" y="140" width="93" height="65" rx="4" fill="#F8FAFF" />
    <path d="M275 141h93m-38-3v-26" stroke="#B7C5D2" strokeWidth="6" />
    <path d="M326 153l-15 23h12l-6 20 20-29h-13z" fill="#B7C5D2" />
  </svg>
}
