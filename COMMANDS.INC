.macro INIT_READ_FLAG 
   .word 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
.endm

.macro DIALOGUE dialogueIndex, spriteIndex, spriteEntranceTransition, spriteExitOrInternalTransition, spriteShake, voiceIndex, textVoiceFont, textSpeed
   .word 1
      .short \dialogueIndex
      .short \spriteIndex
      .short \spriteEntranceTransition
      .short \spriteExitOrInternalTransition
      .short \spriteShake
      .short \voiceIndex
      .short \textVoiceFont
      .short \textSpeed
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
.endm

.macro KBG_DISP kbgIndex
   .word 2
      .short \kbgIndex
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
.endm

.macro PIN_MNL dialogueIndex
   .word 3
      .short \dialogueIndex
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
.endm

.macro BG_DISP bgIndex
   .word 4
      .short \bgIndex
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
.endm

.macro SCREEN_FADEIN timeToFade, unused, fadeLocation, fadeColor
   .word 5
      .short \timeToFade
      .short \unused
      .short \fadeLocation
      .short \fadeColor
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
.endm

.macro SCREEN_FADEOUT timeToFade, unused, fadeColorRed, fadeColorGreen, fadeColorBlue, fadeLocation
   .word 6
      .short \timeToFade
      .short \unused
      .short \fadeColorRed
      .short \fadeColorGreen
      .short \fadeColorBlue
      .short \fadeLocation
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
.endm

.macro SCREEN_FLASH fadeInTime, holdTime, fadeOutTime, flashColorRed, flashColorGreen, flashColorBlue
   .word 7
      .short \fadeInTime
      .short \holdTime
      .short \fadeOutTime
      .short \flashColorRed
      .short \flashColorGreen
      .short \flashColorBlue
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
.endm

.macro SND_PLAY soundIndex, mode, volume, crossfadeDupe, crossfadeTime
   .word 8
      .short \soundIndex
      .short \mode
      .short \volume
      .short \crossfadeDupe
      .short \crossfadeTime
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
.endm

.macro REMOVED 
   .word 9
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
.endm

.macro UNKNOWN0A a, b, c, d, e, f, g, h, i, j, k, l, m, n, o, p
   .word 10
      .short \a
      .short \b
      .short \c
      .short \d
      .short \e
      .short \f
      .short \g
      .short \h
      .short \i
      .short \j
      .short \k
      .short \l
      .short \m
      .short \n
      .short \o
      .short \p
.endm

.macro BGM_PLAY bgmIndex, mode, volume, fadeInTime, fadeOutTime
   .word 11
      .short \bgmIndex
      .short \mode
      .short \volume
      .short \fadeInTime
      .short \fadeOutTime
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
.endm

.macro VCE_PLAY vceIndex
   .word 12
      .short \vceIndex
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
.endm

.macro FLAG flag, set
   .word 13
      .short \flag
      .short \set
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
.endm

.macro TOPIC_GET topicId
   .word 14
      .short \topicId
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
.endm

.macro TOGGLE_DIALOGUE show
   .word 15
      .short \show
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
.endm

.macro SELECT option1, option2, option3, option4, unknown04, unknown05, unknown06, unknown07
   .word 16
      .short \option1
      .short \option2
      .short \option3
      .short \option4
      .short \unknown04
      .short \unknown05
      .short \unknown06
      .short \unknown07
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
.endm

.macro SCREEN_SHAKE duration, horizontalIntensity, verticalIntensity
   .word 17
      .short \duration
      .short \horizontalIntensity
      .short \verticalIntensity
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
.endm

.macro SCREEN_SHAKE_STOP 
   .word 18
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
.endm

.macro GOTO blockId
   .word 19
      .short \blockId
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
.endm

.macro SCENE_GOTO conditionalIndex
   .word 20
      .short \conditionalIndex
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
.endm

.macro WAIT frames
   .word 21
      .short \frames
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
.endm

.macro HOLD 
   .word 22
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
.endm

.macro NOOP1 
   .word 23
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
.endm

.macro VGOTO conditionalIndex, unused, gotoId
   .word 24
      .short \conditionalIndex
      .short \unused
      .short \gotoId
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
.endm

.macro HARUHI_METER unused, addValue, setValue
   .word 25
      .short \unused
      .short \addValue
      .short \setValue
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
.endm

.macro HARUHI_METER_NOSHOW addValue
   .word 26
      .short \addValue
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
.endm

.macro BG_PALEFFECT paletteMode, transitionTime, unknownBool
   .word 27
      .short \paletteMode
      .short \transitionTime
      .short \unknownBool
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
.endm

.macro BG_FADE bgIndex, bgIndexSuper, fadeTime
   .word 28
      .short \bgIndex
      .short \bgIndexSuper
      .short \fadeTime
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
.endm

.macro TRANS_OUT index
   .word 29
      .short \index
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
.endm

.macro TRANS_IN index
   .word 30
      .short \index
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
.endm

.macro SET_PLACE display, placeIndex
   .word 31
      .short \display
      .short \placeIndex
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
.endm

.macro ITEM_DISPIMG itemIndex, x, y
   .word 32
      .short \itemIndex
      .short \x
      .short \y
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
.endm

.macro SET_READ_FLAG 
   .word 33
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
.endm

.macro STOP 
   .word 34
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
.endm

.macro NOOP2 
   .word 35
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
.endm

.macro LOAD_ISOMAP mapFileIndex
   .word 36
      .short \mapFileIndex
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
.endm

.macro INVEST_START unknown00, unknown01, unknown02, unknown03, endScriptBlock
   .word 37
      .short \unknown00
      .short \unknown01
      .short \unknown02
      .short \unknown03
      .short \endScriptBlock
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
.endm

.macro INVEST_END 
   .word 38
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
.endm

.macro CHIBI_EMOTE chibiIndex, emoteIndex
   .word 39
      .short \chibiIndex
      .short \emoteIndex
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
.endm

.macro NEXT_SCENE 
   .word 40
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
.endm

.macro SKIP_SCENE scenesToSkip
   .word 41
      .short \scenesToSkip
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
.endm

.macro GLOBAL globalIndex, value
   .word 42
      .short \globalIndex
      .short \value
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
.endm

.macro CHIBI_ENTEREXIT chibiIndex, mode, delay
   .word 43
      .short \chibiIndex
      .short \mode
      .short \delay
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
.endm

.macro AVOID_DISP 
   .word 44
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
.endm

.macro GLOBAL2D value
   .word 45
      .short \value
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
.endm

.macro CHESS_LOAD chessFileIndex
   .word 46
      .short \chessFileIndex
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
.endm

.macro CHESS_VGOTO clearBlock, missBlock, miss2Block
   .word 47
      .short \clearBlock
      .short \missBlock
      .short \miss2Block
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
.endm

.macro CHESS_MOVE whiteSpaceBegin, whiteSpaceEnd, blackSpaceBegin, blackSpaceEnd
   .word 48
      .short \whiteSpaceBegin
      .short \whiteSpaceEnd
      .short \blackSpaceBegin
      .short \blackSpaceEnd
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
.endm

.macro CHESS_TOGGLE_GUIDE piece1, piece2, piece3, piece4
   .word 49
      .short \piece1
      .short \piece2
      .short \piece3
      .short \piece4
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
.endm

.macro CHESS_TOGGLE_HIGHLIGHT space1, space2, space3, space4, space5, space6, space7, space8, space9, space10, space11, space12, space13, space14, space15, space16
   .word 50
      .short \space1
      .short \space2
      .short \space3
      .short \space4
      .short \space5
      .short \space6
      .short \space7
      .short \space8
      .short \space9
      .short \space10
      .short \space11
      .short \space12
      .short \space13
      .short \space14
      .short \space15
      .short \space16
.endm

.macro CHESS_TOGGLE_CROSS space1, space2, space3, space4, space5, space6, space7, space8, space9, space10, space11, space12, space13, space14, space15, space16
   .word 51
      .short \space1
      .short \space2
      .short \space3
      .short \space4
      .short \space5
      .short \space6
      .short \space7
      .short \space8
      .short \space9
      .short \space10
      .short \space11
      .short \space12
      .short \space13
      .short \space14
      .short \space15
      .short \space16
.endm

.macro CHESS_CLEAR_ANNOTATIONS 
   .word 52
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
.endm

.macro CHESS_RESET 
   .word 53
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
.endm

.macro SCENE_GOTO2 conditionalIndex
   .word 54
      .short \conditionalIndex
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
.endm

.macro EPHEADER headerIndex
   .word 55
      .short \headerIndex
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
.endm

.macro NOOP3 
   .word 56
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
.endm

.macro CONFETTI on
   .word 57
      .short \on
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
.endm

.macro BG_DISPTEMP bgIndex
   .word 58
      .short \bgIndex
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
.endm

.macro UNKNOWN3B a, b, c, d, e, f, g, h, i, j, k, l, m, n, o, p
   .word 59
      .short \a
      .short \b
      .short \c
      .short \d
      .short \e
      .short \f
      .short \g
      .short \h
      .short \i
      .short \j
      .short \k
      .short \l
      .short \m
      .short \n
      .short \o
      .short \p
.endm

.macro OP_MODE 
   .word 60
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
.endm

.macro WAIT_CANCEL frames
   .word 61
      .short \frames
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
.endm

.macro BG_REVERT 
   .word 62
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
.endm

.macro BG_DISP2 bgIndex
   .word 63
      .short \bgIndex
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
      .short 0
.endm

