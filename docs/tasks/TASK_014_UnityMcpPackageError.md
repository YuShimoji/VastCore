# Task: unity-mcp�p�b�P�[�W��Git���|�W�g���G���[����

Status: DONE
Tier: 1
Branch: develop
Owner: Worker
Created: 2025-12-17T12:00:00Z
Report: docs/04_reports/REPORT_014_UnityMcpPackageError_2025-12-17.md 

## Objective
- `com.justinpbarnett.unity-mcp`�p�b�P�[�W��Git���|�W�g���G���[����������
- �p�b�P�[�W������ɓǂݍ��܂���Ԃɂ���A�܂��͕s�v�ȏꍇ�͍폜����

## Context
`com.justinpbarnett.unity-mcp`�p�b�P�[�W��Git���|�W�g���`�F�b�N�A�E�g�ŃG���[�������F
```
Cannot checkout repository [https://github.com/justinpbarnett/unity-mcp.git] on target path [UnityMcpBridge]:
Error when executing git command. error: pathspec 'UnityMcpBridge' did not match any file(s) known to git
```

**�e��**:
- �p�b�P�[�W���ǂݍ��߂Ȃ�
- �ˑ�����@�\���g�p�ł��Ȃ��\��
- Unity�G�f�B�^�[�Ńp�b�P�[�W�����G���[������

## Focus Area
- ���|�W�g���̎��ۂ̍\�����m�F
- �������p�X�����A�܂��̓p�b�P�[�W�̕K�v���𔻒f
- `manifest.json`�̏C���܂��̓p�b�P�[�W�̍폜

## Forbidden Area
- ���̃p�b�P�[�W�ւ̉e�����l�������ɕύX
- �p�b�P�[�W�̍폜�O�Ɉˑ��֌W�̊m�F�����Ȃ�

## Constraints
- �u�����`: develop�u�����`�ō��
- �p�b�P�[�W: Unity Package Manager�̎d�l�ɏ]��

## DoD
- [x] ���|�W�g���̍\�����m�F�iGitHub�Ŋm�F�j����
- [x] �������p�X�����A�܂��̓p�b�P�[�W�̕K�v���𔻒f����
- [x] `manifest.json`���C���A�܂��̓p�b�P�[�W���폜����
- [x] �p�b�P�[�W������ɓǂݍ��܂�邱�Ƃ��m�F����
- [x] �ύX���e��docs/04_reports/��REPORT_*.md�Ƃ��ċL�^
- [x] �{�^�X�N��Report����REPORT�t�@�C���ւ̃����N���L�^

## �������ʁi�������́j

### ���݂̐ݒ�
- `Packages/manifest.json` �Ɉȉ����ݒ肳��Ă���:
  ```json
  "com.justinpbarnett.unity-mcp": "https://github.com/justinpbarnett/unity-mcp.git?path=/UnityMcpBridge"
  ```

### ���_
- ���|�W�g������ `UnityMcpBridge` �p�X�����݂��Ȃ��\��
- �p�b�P�[�W�̃T�u�f�B���N�g���p�X���Ԉ���Ă���\��

## ������i��āj

### �I�v�V����1: �������p�X����肵�ďC��
1. GitHub���|�W�g���̍\�����m�F
2. �������T�u�f�B���N�g���p�X�����
3. `manifest.json`�̃p�X���C��

### �I�v�V����2: �p�b�P�[�W���s�v�ȏꍇ�͍폜
1. �v���W�F�N�g���Ńp�b�P�[�W�̎g�p�ӏ����m�F
2. �g�p����Ă��Ȃ��ꍇ��`manifest.json`����폜
3. �g�p����Ă���ꍇ�͑�֎�i������

### �I�v�V����3: �p�b�P�[�W�̃o�[�W����/�^�O���w��
1. ���|�W�g���̃^�O/�u�����`���m�F
2. ����̃o�[�W����/�u�����`���w�肵�ēǂݍ���

## �֘A�t�@�C��
- `Packages/manifest.json`

## Notes
- Status �� OPEN / IN_PROGRESS / BLOCKED / DONE ��z��
- BLOCKED �̏ꍇ�́A�u���b�N���R/������/����𖾊m�ɂ��AReport �� docs/inbox/REPORT_*.md ���L�^
- ���̃^�X�N�͒P�Ƃ�Worker�Ŋ����\
