/** Original vector artwork; all frames share an anchor and palette. */
export function cowFrame(patch: string, stride = 0, grazing = false): string {
  const head = grazing ? 'translate(2 9) rotate(10 94 66)' : '';
  return `<svg xmlns="http://www.w3.org/2000/svg" width="160" height="140" viewBox="0 0 160 140">
    <ellipse cx="78" cy="119" rx="49" ry="10" fill="#52613d" opacity=".16"/>
    <path d="M39 69 Q22 67 27 93" fill="none" stroke="#82776a" stroke-width="4" stroke-linecap="round"/>
    <path d="M26 89 L22 99 L29 96" fill="${patch}"/>
    <g fill="#dfdbcf" stroke="#837d70" stroke-width="1.5">
      <path d="M48 87 L${46 + stride} 113 Q${51 + stride} 119 ${57 + stride} 113 L62 86Z"/>
      <path d="M91 91 L${92 - stride} 116 Q${97 - stride} 120 ${103 - stride} 115 L105 85Z"/>
    </g>
    <path d="M39 57 Q53 39 88 47 Q117 50 121 74 Q124 99 92 101 L52 95 Q30 88 39 57Z" fill="#f7f4e8" stroke="#a8a190" stroke-width="1.8"/>
    <path d="M41 62 Q42 49 57 48 L72 49 Q78 67 63 72 Q48 79 41 62Z M86 47 Q107 50 112 60 Q104 69 92 65 Q81 60 86 47Z" fill="${patch}"/>
    <path d="M45 85 L${44 - stride} 115 Q${50 - stride} 121 ${57 - stride} 115 L58 88Z M98 90 L${99 + stride} 117 Q${105 + stride} 122 ${112 + stride} 117 L112 88Z" fill="#f7f4e8" stroke="#a8a190" stroke-width="1.8"/>
    <path d="M${44 - stride} 112 L${57 - stride} 113 L${57 - stride} 117 Q${50 - stride} 122 ${44 - stride} 117Z M${99 + stride} 114 L${112 + stride} 115 L${112 + stride} 119 Q${105 + stride} 123 ${99 + stride} 119Z" fill="${patch}"/>
    <g transform="${head}">
      <path d="M100 48 L94 33 Q101 31 107 45 M121 47 L126 34 Q133 34 129 53" fill="#dbccb1" stroke="#a89d87" stroke-width="1.5"/>
      <path d="M103 52 Q85 39 84 50 Q85 63 103 64 M124 55 Q143 43 144 56 Q141 66 125 66" fill="${patch}" stroke="#9b9484" stroke-width="1.5"/>
      <path d="M102 46 Q126 40 132 65 L128 86 Q113 102 97 87 L95 63 Q95 52 102 46Z" fill="#f7f4e8" stroke="#a8a190" stroke-width="1.8"/>
      <path d="M106 47 Q96 46 97 62 L100 71 Q112 76 116 63 L115 47Z" fill="${patch}"/>
      <ellipse cx="116" cy="86" rx="22" ry="14" fill="#dcc2ac" stroke="#b69e86" stroke-width="1.5"/>
      <ellipse cx="106" cy="65" rx="3" ry="4" fill="#353b32"/><ellipse cx="126" cy="66" rx="3" ry="4" fill="#353b32"/>
      <circle cx="107" cy="64" r="1" fill="white"/><circle cx="127" cy="65" r="1" fill="white"/>
      <ellipse cx="107" cy="85" rx="2.5" ry="3" fill="#927965"/><ellipse cx="123" cy="86" rx="2.5" ry="3" fill="#927965"/>
      <path d="M111 93 Q117 97 122 93" fill="none" stroke="#927965" stroke-width="1.5" stroke-linecap="round"/>
    </g>
  </svg>`;
}

export const treeArt = `<svg xmlns="http://www.w3.org/2000/svg" width="120" height="150" viewBox="0 0 120 150">
  <ellipse cx="60" cy="134" rx="39" ry="10" fill="#52613d" opacity=".15"/>
  <path d="M55 76 L67 77 L65 134 L54 133Z" fill="#88765d"/>
  <path d="M60 105 L40 83 M61 98 L79 74" stroke="#88765d" stroke-width="6"/>
  <path d="M31 90 Q6 68 26 49 Q22 25 48 27 Q68 9 85 31 Q111 32 105 57 Q125 84 98 95 Q68 111 31 90Z" fill="#8eaa74"/>
  <path d="M25 70 Q35 90 62 88 Q89 88 104 64 Q114 91 93 97 Q48 112 25 88Z" fill="#7d9766"/>
</svg>`;

export const shedArt = `<svg xmlns="http://www.w3.org/2000/svg" width="160" height="140" viewBox="0 0 160 140">
  <ellipse cx="80" cy="122" rx="66" ry="12" fill="#52613d" opacity=".13"/>
  <path d="M29 63 L85 84 L85 128 L29 104Z" fill="#ddd5bc"/>
  <path d="M85 84 L134 57 L134 104 L85 128Z" fill="#bcb69f"/>
  <path d="M85 84 L52 30 L20 61Z" fill="#ebe3cb"/>
  <path d="M52 30 L102 6 L145 57 L85 88Z" fill="#a58d72"/>
  <path d="M52 30 L20 61 L14 56 L49 23 L102 0 L107 6Z" fill="#8c775f"/>
  <path d="M43 80 L66 89 L66 120 L43 111Z" fill="#716957"/>
  <path d="M103 81 L120 72 L120 91 L103 100Z" fill="#776f5d"/>
</svg>`;
