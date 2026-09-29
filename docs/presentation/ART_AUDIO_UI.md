# Art, audio и UI direction

**Status:** broad direction е `DECIDED`; detailed style guide е `PROPOSED/OPEN`; current assets са placeholder.

## Art

Dark, atmospheric, grounded, stylized/semi-realistic 3D; не AAA photorealism. Candidate traits: controlled lighting, strong shadows, rain/fog/grain, readable silhouettes, inspectable details и old/stylized interfaces.

### Must happen

- Gameplay-relevant details са readable.
- Darkness supports uncertainty без unfair hiding.
- Materials/lighting помагат на observation.
- Placeholder primitives са приемливи до нужда от representative art.

### Must not happen

- Photorealism за сметка на scope.
- Darkness да прикрива interaction problems.
- Genre effects без readability checks.
- Final art преди spatial/gameplay stability.

## Audio

Rain, traffic, phone, computer hum, lights, footsteps, clock, radio, doors, room tone и silence поддържат presence/tension.

### Must happen

- Audio потвърждава action без clue importance.
- Spatial audio само когато location matters.
- Dialogue intelligibility пред ambience.

### Must not happen

- Musical sting посочва correct clue/liar.
- Constant ambience маскира speech/feedback.
- Full voice production преди dialogue validation.

## UI/UX

Diegetic когато е полезно, но clarity има priority. Consistent entry/navigation/exit, readable text, scalable layout, contrast и source/uncertainty preservation.

### Език

- По `D-019` цялото player-facing съдържание е на български: menus, buttons, labels, dialogue, subtitles, documents, case records, emails и interaction feedback.
- C# identifiers, filenames, Unity API имена, stable record IDs и утвърдени technical terms не се превеждат, когато това би счупило references или би намалило precision.
- Всеки използван UI font трябва да има проверена кирилица. Липсващите glyphs, квадратчета или смесена латиница/кирилица са release-blocking UI defect.
- Имена на хора и fictional locations се изписват на кирилица в player-facing content; служебни IDs като `CASE-0017` могат да останат на латиница.

### Must not happen

- Color/animation издава correct deduction.
- Tiny text за fake realism.
- Pixel-perfect clicking.
- Duplicate information без gameplay reason.
- Polish да се представя като core-loop validation.
- Английски placeholder текст в player-facing UI.
