﻿using UnityEngine;

namespace Game.Cards.PlaygroundCards {
    [CreateAssetMenu(fileName = "new_playgroud_card", menuName = "DarkDust/Playground Card", order = 0)]
    public class PlaygroundCardData : ScriptableObject {
        [SerializeField] private PlaygroundCardType _cardType;
        [SerializeField] private CardDirection _cardDirection = CardDirection.None;
        [SerializeField] private Sprite _frontImage;
        [SerializeField] private Sprite _backImage;
        [SerializeField] private int _cardCount;

        public Sprite BackImage => _backImage;
        public Sprite FrontImage => _frontImage;
        public int CardCount => _cardCount;
        public PlaygroundCardType CardType => _cardType;
        public CardDirection CardDirection => _cardDirection;
    }
}